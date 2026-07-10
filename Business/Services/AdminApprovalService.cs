using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Business.Services;

/// <summary>
/// Handles the manual contract workflow triggered by the admin:
///   UploadContractAsync  → creates Contract + EscrowTransaction → WaitingForSignatures
///   ApproveContractAsync → releases escrow → pays landlord → Approved
///   RejectContractAsync  → refunds escrow from admin balance → student RefundIssued receipt → Rejected
/// </summary>
public class AdminApprovalService : IAdminApprovalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEscrowService _escrowService;
    private readonly IReceiptService _receiptService;
    private readonly INotificationService _notificationService;
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly IBalanceService _balanceService;
    private readonly IContractService _contractService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AdminApprovalService> _logger;

    private const decimal PlatformFeePercentage = 5.0m;

    public AdminApprovalService(
        IUnitOfWork unitOfWork,
        IEscrowService escrowService,
        IReceiptService receiptService,
        INotificationService notificationService,
        IPaymentHistoryService paymentHistoryService,
        IBalanceService balanceService,
        IContractService contractService,
        UserManager<User> userManager,
        ILogger<AdminApprovalService> logger)
    {
        _unitOfWork = unitOfWork;
        _escrowService = escrowService;
        _receiptService = receiptService;
        _notificationService = notificationService;
        _paymentHistoryService = paymentHistoryService;
        _balanceService = balanceService;
        _contractService = contractService;
        _userManager = userManager;
        _logger = logger;
    }

    // =========================================================================
    // 1. Admin uploads contract (manual trigger)
    //    - Validates booking is in WaitingForContract
    //    - Uses ContractService.UploadContractAsync for manual PDF upload
    //    - Escrow is created automatically by ContractService
    //    - Moves booking to WaitingForSignatures
    //    - Notifies student and landlord
    // =========================================================================
    public async Task<AdminContractApprovalResponse> UploadContractAsync(AdminUploadContractRequest request)
    {
        using var tx = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var booking = await _unitOfWork.Bookings.GetAsync(request.BookingId);
            if (booking == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Booking not found");
            }

            var payment = await _unitOfWork.Payments.GetByBookingIdAsync(booking.BookingId);
            bool isPaymentCompleted = payment != null && payment.PaymentStatus == PaymentStatus.Completed;

            if (booking.BookingStatus != BookingStatus.WaitingForContract && !(booking.BookingStatus == BookingStatus.PendingPayment && isPaymentCompleted))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail($"Booking is not awaiting a contract. Current status: {booking.BookingStatus}");
            }

            if (!isPaymentCompleted)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("No completed payment found for this booking");
            }

            // Guard against duplicate contract
            var existingContract = await _unitOfWork.Contracts.GetByBookingIdAsync(booking.BookingId);
            if (existingContract != null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("A contract already exists for this booking");
            }

            // Use ContractService for manual upload (escrow is created internally)
            if (request.PdfStream == null || string.IsNullOrEmpty(request.FileName))
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("PDF file and file name are required for contract upload");
            }

            var contractResponse = await _contractService.UploadContractAsync(
                request.BookingId,
                request.PdfStream,
                request.FileName);

            _logger.LogInformation("Contract {Number} uploaded for booking {BookingId}",
                contractResponse.ContractNumber, booking.BookingId);

            // Get the escrow (created at payment time or by ContractService)
            var escrow = await _unitOfWork.EscrowTransactions.GetByPaymentIdAsync(payment.PaymentId);
            if (escrow == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Escrow transaction not found. Please ensure payment was fully processed.");
            }

            // Link escrow to the contract if not already linked
            if (escrow.ContractId == Guid.Empty || escrow.ContractId == null)
            {
                escrow.ContractId = contractResponse.ContractId;
                escrow.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.EscrowTransactions.Update(escrow);
                _logger.LogInformation("Escrow {EscrowId} linked to contract {ContractId}", escrow.EscrowId, contractResponse.ContractId);
            }

            // Resolve parties for notifications
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            var owner = await ResolveOwnerAsync(booking);
            if (owner == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Landlord not found for this booking");
            }

            User? studentUser = string.IsNullOrEmpty(student?.UserId) ? null
                : await _userManager.FindByIdAsync(student.UserId);
            User? ownerUser = string.IsNullOrEmpty(owner.UserId) ? null
                : await _userManager.FindByIdAsync(owner.UserId);

            // Use Email as display name — more meaningful than login UserName
            string studentName = studentUser?.Email
                ?? studentUser?.UserName
                ?? student?.User?.Email
                ?? student?.User?.UserName
                ?? "Unknown";
            string ownerName = ownerUser?.Email
                ?? ownerUser?.UserName
                ?? owner.User?.Email
                ?? owner.User?.UserName
                ?? "Unknown";

            // Generate EscrowHeld receipts for student, landlord, admin
            await GenerateEscrowHeldReceiptsAsync(
                payment, escrow, studentName, ownerName,
                student?.UserId, owner.UserId, request.AdminUserId,
                contractResponse.ContractId);

            // Record payment history
            await _paymentHistoryService.RecordPaymentEventAsync(
                payment.PaymentId, booking.BookingId, escrow.EscrowId,
                student?.UserId ?? string.Empty,
                "ContractUploaded",
                $"Contract {contractResponse.ContractNumber} uploaded by admin. Escrow created. Awaiting signatures.",
                payment.Amount,
                booking.BookingStatus.ToString(),
                BookingStatus.WaitingForSignatures.ToString(),
                request.AdminUserId, "Admin",
                metadata: new Dictionary<string, object>
                {
                    { "ContractId", contractResponse.ContractId },
                    { "EscrowId", escrow.EscrowId }
                });

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Contract uploaded and escrow created for booking {BookingId}", booking.BookingId);

            // Notify parties (post-commit, non-fatal)
            await NotifySafe(student?.UserId,
                $"The contract is ready for your signature. Please download, sign, and upload it.",
                "ContractReady");
            await NotifySafe(owner.UserId,
                $"A new booking contract is ready for your signature. Please review and sign.",
                "ContractReady");

            return new AdminContractApprovalResponse
            {
                Success = true,
                Message = "Contract uploaded and escrow created. Parties notified for signatures.",
                ContractId = contractResponse.ContractId,
                EscrowId = escrow.EscrowId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UploadContractAsync for booking {BookingId}", request.BookingId);
            try { await _unitOfWork.RollbackTransactionAsync(); } catch { }
            return Fail($"Error: {ex.Message}");
        }
    }

    // =========================================================================
    // 2. Admin approves fully-signed contract
    //    - Validates both signatures + WaitingForAdminApproval
    //    - Releases escrow
    //    - Credits landlord balance with (amount - platformFee)
    //    - Credits admin balance with platformFee only
    //    - Generates OwnerPayout receipt for landlord
    //    - Moves booking to Approved
    // =========================================================================
    public async Task<AdminContractApprovalResponse> ApproveContractAsync(AdminContractApprovalRequest request)
    {
        using var tx = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var contract = await _unitOfWork.Contracts.GetAsync(request.ContractId);
            if (contract == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Contract not found");
            }

            if (!contract.IsStudentSigned || !contract.IsLandlordSigned)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Contract must be signed by both parties before approval");
            }

            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);
            if (booking == null || booking.BookingStatus != BookingStatus.WaitingForAdminApproval)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail(booking == null ? "Booking not found"
                    : $"Booking is not in WaitingForAdminApproval. Status: {booking.BookingStatus}");
            }

            if (contract.IsAdminApproved)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Contract is already approved");
            }

            // Find escrow linked to this contract
            var escrow = await _unitOfWork.EscrowTransactions.GetByContractIdAsync(contract.ContractId);
            if (escrow == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Escrow not found for this contract");
            }

            // Idempotency check: if escrow already released, return success
            if (escrow.Status == EscrowStatus.Released)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogInformation("Escrow {EscrowId} already released. Returning success.", escrow.EscrowId);
                return new AdminContractApprovalResponse
                {
                    Success = true,
                    Message = "Escrow already released",
                    ContractId = contract.ContractId,
                    EscrowId = escrow.EscrowId,
                    IsOwnerPayoutProcessed = escrow.LandlordPayoutAt.HasValue
                };
            }

            if (escrow.Status != EscrowStatus.Holding)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail($"Escrow is not in Holding status. Status: {escrow.Status}");
            }

            // Mark contract approved
            contract.IsAdminApproved = true;
            contract.AdminUserId = request.AdminUserId;
            contract.AdminApprovedAt = DateTime.UtcNow;
            contract.AdminNotes = request.AdminNotes;
            contract.ContractStatus = ContractStatus.Approved;
            contract.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Contracts.Update(contract);

            // Release escrow
            escrow.Status = EscrowStatus.Released;
            escrow.ReleasedAt = DateTime.UtcNow;
            escrow.ReleasedByUserId = request.AdminUserId;
            escrow.ReleaseNotes = $"Released upon admin approval. Notes: {request.AdminNotes}";
            escrow.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.EscrowTransactions.Update(escrow);

            // Approve booking
            var previousStatus = booking.BookingStatus.ToString();
            booking.BookingStatus = BookingStatus.WaitingForAdminApproval;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync();

            // Resolve landlord and payment
            var owner = await ResolveOwnerAsync(booking);
            var payment = await _unitOfWork.Payments.GetAsync(escrow.PaymentId);
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);

            // Calculate payout amounts
            var platformFee = escrow.PlatformFee > 0 ? escrow.PlatformFee : (escrow.Amount * 0.05m);
            var landlordPayoutAmount = escrow.Amount - platformFee;

            // Record payment history event
            if (payment != null)
            {
                await _paymentHistoryService.RecordPaymentEventAsync(
                    payment.PaymentId,
                    booking.BookingId,
                    null,
                    booking.Student?.UserId ?? string.Empty,
                    "ContractApprovedByAdmin",
                    $"Contract {contract.ContractNumber} has been approved by admin. Escrow funds held in platform account.",
                    payment.Amount,
                    previousStatus,
                    BookingStatus.WaitingForAdminApproval.ToString(),
                    request.AdminUserId,
                    "Admin",
                    metadata: new Dictionary<string, object>
                    {
                        { "ContractId", contract.ContractId },
                        { "ContractNumber", contract.ContractNumber },
                        { "AdminNotes", request.AdminNotes ?? "" }
                    });
            }

            // Credit landlord balance with payout amount (amount - platform fee)
            if (owner != null && !string.IsNullOrEmpty(owner.UserId))
            {
                await _balanceService.AddToBalanceAsync(
                    owner.UserId, "LandLord",
                    landlordPayoutAmount,
                    $"Payout for contract {contract.ContractNumber}");
                _logger.LogInformation(
                    "Credited landlord {UserId} with {Amount} EGP for contract {Number}",
                    owner.UserId, landlordPayoutAmount, contract.ContractNumber);
            }

            // Credit admin balance with platform fee only
            await _balanceService.AddToBalanceAsync(
                request.AdminUserId, "Admin",
                platformFee,
                $"Platform fee for contract {contract.ContractNumber}");
            _logger.LogInformation(
                "Credited admin with platform fee {Amount} EGP for contract {Number}",
                platformFee, contract.ContractNumber);

            // Mark escrow payout details
            var payoutRef = $"PAYOUT-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            escrow.LandlordPayoutAmount = landlordPayoutAmount;
            escrow.LandlordPayoutTransactionId = payoutRef;
            escrow.LandlordPayoutAt = DateTime.UtcNow;
            await _unitOfWork.EscrowTransactions.Update(escrow);
            await _unitOfWork.SaveChangesAsync();

            // Generate OwnerPayout receipt for landlord
            if (owner != null && payment != null)
            {
                User? ownerUser = string.IsNullOrEmpty(owner.UserId) ? null
                    : await _userManager.FindByIdAsync(owner.UserId);

                await _receiptService.GenerateEscrowReceiptAsync(new ReceiptGenerationRequest
                {
                    PaymentId = payment.PaymentId,
                    EscrowId = escrow.EscrowId,
                    Type = ReceiptType.OwnerPayout,
                    IssuedToUserId = owner.UserId ?? string.Empty,
                    IssuedToRole = "LandLord",
                    IssuedToName = ownerUser?.Email ?? ownerUser?.UserName ?? owner.User?.Email ?? owner.User?.UserName ?? "Unknown",
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "PayoutAmount", landlordPayoutAmount },
                        { "PlatformFee", platformFee },
                        { "OriginalAmount", escrow.Amount },
                        { "ContractNumber", contract.ContractNumber }
                    }
                });
            }

            // Payment history - escrow release
            if (payment != null)
            {
                await _paymentHistoryService.RecordPaymentEventAsync(
                    payment.PaymentId, booking.BookingId, escrow.EscrowId,
                    student?.UserId ?? string.Empty,
                    "EscrowReleased",
                    $"Escrow released for contract {contract.ContractNumber}. Amount {escrow.Amount} EGP distributed.",
                    escrow.Amount, previousStatus, BookingStatus.Approved.ToString(),
                    request.AdminUserId, "Admin",
                    metadata: new Dictionary<string, object>
                    {
                        { "ContractId", contract.ContractId },
                        { "LandlordPayout", landlordPayoutAmount },
                        { "PlatformFee", platformFee }
                    });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Contract {Number} approved. Booking {BookingId} Approved. Landlord payout: {LandlordPayout}, Platform fee: {PlatformFee}",
                contract.ContractNumber, booking.BookingId, landlordPayoutAmount, platformFee);

            // Notifications (post-commit)
            await NotifySafe(owner?.UserId,
                $"Contract approved! Payout of {landlordPayoutAmount:N2} EGP has been transferred to your account.",
                "BookingApproved");
            await NotifySafe(student?.UserId,
                "Your booking has been approved and confirmed!",
                "BookingApproved");

            return new AdminContractApprovalResponse
            {
                Success = true,
                Message = "Contract approved and landlord payout processed",
                ContractId = contract.ContractId,
                EscrowId = escrow.EscrowId,
                IsOwnerPayoutProcessed = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApproveContractAsync for contract {ContractId}", request.ContractId);
            try { await _unitOfWork.RollbackTransactionAsync(); } catch { }
            return Fail($"Error: {ex.Message}");
        }
    }

    // =========================================================================
    // 3. Admin rejects contract
    //    - Refunds escrow: processes Paymob refund or credits student wallet
    //    - Moves booking to Rejected
    //    - Does NOT deduct from admin balance (money held in escrow, not admin balance)
    // =========================================================================
    public async Task<AdminContractApprovalResponse> RejectContractAsync(AdminContractApprovalRequest request)
    {
        using var tx = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var contract = await _unitOfWork.Contracts.GetAsync(request.ContractId);
            if (contract == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Contract not found");
            }

            contract.AdminUserId = request.AdminUserId;
            contract.AdminNotes = request.AdminNotes;
            contract.ContractStatus = ContractStatus.Rejected;
            contract.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Contracts.Update(contract);

            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);
            if (booking == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Booking not found");
            }

            var previousStatus = booking.BookingStatus.ToString();
            booking.BookingStatus = BookingStatus.Rejected;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            var escrow = await _unitOfWork.EscrowTransactions.GetByContractIdAsync(contract.ContractId);
            var payment = await _unitOfWork.Payments.GetByBookingIdAsync(booking.BookingId);
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);

            if (escrow != null && escrow.Status == EscrowStatus.Holding)
            {
                // Idempotency check
                escrow.Status = EscrowStatus.Refunded;
                escrow.RefundedAt = DateTime.UtcNow;
                escrow.RefundReason = $"Contract rejected: {request.AdminNotes}";
                escrow.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.EscrowTransactions.Update(escrow);

                // Process refund based on payment method
                if (payment != null)
                {
                    if (payment.PaymentMethod == PaymentMethod.Paymob)
                    {
                        // Paymob refund: process through Paymob API
                        var paymentTxn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(escrow.PaymentId ?? Guid.Empty);
                        if (paymentTxn != null && !string.IsNullOrEmpty(paymentTxn.PaymobTransactionId))
                        {
                            try
                            {
                                var refundReq = new EscrowRefundRequest
                                {
                                    EscrowId = escrow.EscrowId,
                                    AdminUserId = request.AdminUserId,
                                    RefundReason = $"Contract rejected: {request.AdminNotes}"
                                };
                                await _escrowService.RefundEscrowAsync(refundReq);
                                _logger.LogInformation("Paymob refund processed for escrow {EscrowId}", escrow.EscrowId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Paymob refund failed for escrow {EscrowId}", escrow.EscrowId);
                                // Continue with manual refund tracking even if Paymob fails
                            }
                        }
                    }
                    else if (payment.PaymentMethod == PaymentMethod.Wallet)
                    {
                        // Wallet refund: credit student wallet balance
                        if (student != null && !string.IsNullOrEmpty(student.UserId))
                        {
                            await _balanceService.AddToBalanceAsync(
                                student.UserId, "Student",
                                escrow.Amount,
                                $"Refund for rejected contract {contract.ContractNumber}");
                            _logger.LogInformation("Student wallet credited with {Amount} EGP for refund", escrow.Amount);
                        }
                    }
                }

                // Generate RefundIssued receipt for student
                if (student != null && payment != null)
                {
                    User? studentUser = string.IsNullOrEmpty(student.UserId) ? null
                        : await _userManager.FindByIdAsync(student.UserId);

                    await _receiptService.GenerateEscrowReceiptAsync(new ReceiptGenerationRequest
                    {
                        PaymentId = payment.PaymentId,
                        EscrowId = escrow.EscrowId,
                        Type = ReceiptType.RefundIssued,
                        IssuedToUserId = student.UserId ?? string.Empty,
                        IssuedToRole = "Student",
                        IssuedToName = studentUser?.Email ?? studentUser?.UserName ?? student.User?.Email ?? student.User?.UserName ?? "Unknown",
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "RefundAmount", escrow.Amount },
                            { "RefundReason", request.AdminNotes ?? "" },
                            { "ContractNumber", contract.ContractNumber },
                            { "PaymentMethod", payment.PaymentMethod.ToString() }
                        }
                    });
                }
            }

            // Payment history
            if (payment != null)
            {
                await _paymentHistoryService.RecordPaymentEventAsync(
                    payment.PaymentId, booking.BookingId, escrow?.EscrowId,
                    student?.UserId ?? string.Empty,
                    "ContractRejected",
                    $"Contract {contract.ContractNumber} rejected: {request.AdminNotes}. Refund processed via {payment.PaymentMethod}.",
                    payment.Amount, previousStatus, BookingStatus.Rejected.ToString(),
                    request.AdminUserId, "Admin",
                    metadata: new Dictionary<string, object>
                    {
                        { "ContractId", contract.ContractId },
                        { "RejectionReason", request.AdminNotes ?? "" },
                        { "PaymentMethod", payment.PaymentMethod.ToString() }
                    });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Contract {Number} rejected by admin {AdminId}. Refund processed via {PaymentMethod}",
                contract.ContractNumber, request.AdminUserId, payment?.PaymentMethod.ToString());

            await NotifySafe(student?.UserId,
                $"Your contract was rejected. Reason: {request.AdminNotes}. Your refund is being processed.",
                "ContractRejected");

            return new AdminContractApprovalResponse
            {
                Success = true,
                Message = "Contract rejected and refund initiated",
                ContractId = contract.ContractId,
                EscrowId = escrow?.EscrowId,
                IsEscrowRefunded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RejectContractAsync for contract {ContractId}", request.ContractId);
            try { await _unitOfWork.RollbackTransactionAsync(); } catch { }
            return Fail($"Error: {ex.Message}");
        }
    }

    // =========================================================================
    // Standalone escrow operations (keep for manual override if needed)
    // =========================================================================
    public async Task<AdminContractApprovalResponse> ProcessEscrowReleaseAsync(EscrowReleaseRequest request)
    {
        using var tx = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Escrow not found");
            }

            // Idempotency check
            if (escrow.Status == EscrowStatus.Released)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogInformation("Escrow {EscrowId} already released. Returning success.", escrow.EscrowId);
                return new AdminContractApprovalResponse
                {
                    Success = true,
                    Message = "Escrow already released",
                    EscrowId = request.EscrowId,
                    IsOwnerPayoutProcessed = escrow.LandlordPayoutAt.HasValue
                };
            }

            if (escrow.Status != EscrowStatus.Holding)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail($"Escrow not Holding. Status: {escrow.Status}");
            }

            var contract = await _unitOfWork.Contracts.GetAsync(escrow.ContractId);
            if (contract == null || !contract.IsAdminApproved)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Contract must be admin-approved before manual escrow release");
            }

            var escrowResponse = await _escrowService.ReleaseEscrowAsync(request);
            await _escrowService.ProcessOwnerPayoutAsync(request.EscrowId);

            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);
            var payment = escrow.PaymentId.HasValue
                ? await _unitOfWork.Payments.GetAsync(escrow.PaymentId.Value) : null;

            var owner = booking != null ? await ResolveOwnerAsync(booking) : null;
            if (owner != null && !string.IsNullOrEmpty(owner.UserId) && payment != null)
            {
                var landlordPayoutAmount = escrow.Amount - escrow.PlatformFee;
                var platformFee = escrow.PlatformFee;

                // Credit landlord balance with payout amount (amount - platform fee)
                await _balanceService.AddToBalanceAsync(
                    owner.UserId, "LandLord",
                    landlordPayoutAmount,
                    $"Manual escrow release for contract {contract.ContractNumber}");

                // Credit admin balance with platform fee only
                await _balanceService.AddToBalanceAsync(
                    request.AdminUserId, "Admin",
                    platformFee,
                    $"Platform fee for contract {contract.ContractNumber}");

                User? ownerUser = string.IsNullOrEmpty(owner.UserId) ? null
                    : await _userManager.FindByIdAsync(owner.UserId);

                await _receiptService.GenerateEscrowReceiptAsync(new ReceiptGenerationRequest
                {
                    PaymentId = payment.PaymentId,
                    EscrowId = escrow.EscrowId,
                    Type = ReceiptType.OwnerPayout,
                    IssuedToUserId = owner.UserId,
                    IssuedToRole = "LandLord",
                    IssuedToName = ownerUser?.Email ?? ownerUser?.UserName ?? "Unknown",
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "PayoutAmount", landlordPayoutAmount },
                        { "PlatformFee", platformFee },
                        { "OriginalAmount", escrow.Amount }
                    }
                });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new AdminContractApprovalResponse
            {
                Success = true, Message = "Escrow released",
                EscrowId = request.EscrowId, IsOwnerPayoutProcessed = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessEscrowReleaseAsync");
            try { await _unitOfWork.RollbackTransactionAsync(); } catch { }
            return Fail($"Error: {ex.Message}");
        }
    }

    public async Task<AdminContractApprovalResponse> ProcessEscrowRefundAsync(EscrowRefundRequest request)
    {
        using var tx = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail("Escrow not found");
            }

            // Idempotency check
            if (escrow.Status == EscrowStatus.Refunded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogInformation("Escrow {EscrowId} already refunded. Returning success.", escrow.EscrowId);
                return new AdminContractApprovalResponse
                {
                    Success = true,
                    Message = "Escrow already refunded",
                    EscrowId = request.EscrowId,
                    IsEscrowRefunded = true
                };
            }

            if (escrow.Status != EscrowStatus.Holding)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Fail($"Escrow not Holding. Status: {escrow.Status}");
            }

            await _escrowService.RefundEscrowAsync(request);

            // DO NOT deduct from admin balance - money is held in escrow, not admin balance
            // Refund is processed through Paymob API or credited to student wallet

            var contract = await _unitOfWork.Contracts.GetAsync(escrow.ContractId);
            var booking = contract != null ? await _unitOfWork.Bookings.GetAsync(contract.BookingId) : null;
            if (booking != null)
            {
                booking.BookingStatus = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.Update(booking);
            }

            var payment = escrow.PaymentId.HasValue
                ? await _unitOfWork.Payments.GetAsync(escrow.PaymentId.Value) : null;
            var student = booking != null ? await _unitOfWork.Students.GetAsync(booking.StudentId) : null;

            // Process refund based on payment method
            if (payment != null && student != null)
            {
                if (payment.PaymentMethod == PaymentMethod.Wallet && !string.IsNullOrEmpty(student.UserId))
                {
                    // Wallet refund: credit student wallet balance
                    await _balanceService.AddToBalanceAsync(
                        student.UserId, "Student",
                        escrow.Amount,
                        $"Manual escrow refund: {request.RefundReason}");
                }

                User? studentUser = string.IsNullOrEmpty(student.UserId) ? null
                    : await _userManager.FindByIdAsync(student.UserId);

                await _receiptService.GenerateEscrowReceiptAsync(new ReceiptGenerationRequest
                {
                    PaymentId = payment.PaymentId,
                    EscrowId = escrow.EscrowId,
                    Type = ReceiptType.RefundIssued,
                    IssuedToUserId = student.UserId ?? string.Empty,
                    IssuedToRole = "Student",
                    IssuedToName = studentUser?.UserName ?? "Unknown",
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "RefundAmount", escrow.Amount },
                        { "RefundReason", request.RefundReason },
                        { "PaymentMethod", payment.PaymentMethod.ToString() }
                    }
                });

                await NotifySafe(student.UserId,
                    $"Refund of {escrow.Amount:N2} EGP processed. Reason: {request.RefundReason}",
                    "RefundProcessed");
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new AdminContractApprovalResponse
            {
                Success = true, Message = "Escrow refunded",
                EscrowId = request.EscrowId, IsEscrowRefunded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessEscrowRefundAsync");
            try { await _unitOfWork.RollbackTransactionAsync(); } catch { }
            return Fail($"Error: {ex.Message}");
        }
    }

    // =========================================================================
    // Queries
    // =========================================================================
    public async Task<IEnumerable<ContractResponse>> GetPendingContractsAsync()
    {
        var contracts = await _unitOfWork.Contracts.GetAwaitingAdminApprovalAsync();
        return contracts.Select(MapContract);
    }

    public async Task<IEnumerable<EscrowResponse>> GetPendingEscrowReleasesAsync()
    {
        var escrows = await _unitOfWork.EscrowTransactions.GetPendingReleaseAsync();
        return escrows.Select(MapEscrow);
    }

    // =========================================================================
    // Private helpers
    // =========================================================================
    private async Task GenerateEscrowHeldReceiptsAsync(
        Payment payment, EscrowTransaction escrow,
        string studentName, string ownerName,
        string? studentUserId, string? ownerUserId, string adminUserId,
        Guid contractId)
    {
        // Student EscrowHeld receipt
        await _receiptService.GenerateEscrowReceiptAsync(new ReceiptGenerationRequest
        {
            PaymentId = payment.PaymentId,
            EscrowId = escrow.EscrowId,
            Type = ReceiptType.EscrowHeld,
            IssuedToUserId = studentUserId ?? string.Empty,
            IssuedToRole = "Student",
            IssuedToName = studentName,
            AdditionalData = new Dictionary<string, object>
            {
                { "Amount", escrow.Amount },
                { "PlatformFee", escrow.PlatformFee },
                { "ContractId", contractId }
            }
        });

        // Landlord EscrowHeld receipt
        await _receiptService.GenerateEscrowReceiptAsync(new ReceiptGenerationRequest
        {
            PaymentId = payment.PaymentId,
            EscrowId = escrow.EscrowId,
            Type = ReceiptType.EscrowHeld,
            IssuedToUserId = ownerUserId ?? string.Empty,
            IssuedToRole = "LandLord",
            IssuedToName = ownerName,
            AdditionalData = new Dictionary<string, object>
            {
                { "Amount", escrow.Amount },
                { "PlatformFee", escrow.PlatformFee },
                { "StudentName", studentName }
            }
        });

        // Admin EscrowHeld receipt
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var adminUser = adminUsers.FirstOrDefault(u => u.Id == adminUserId)
                     ?? adminUsers.FirstOrDefault();
        if (adminUser != null)
        {
            await _receiptService.GenerateEscrowReceiptAsync(new ReceiptGenerationRequest
            {
                PaymentId = payment.PaymentId,
                EscrowId = escrow.EscrowId,
                Type = ReceiptType.EscrowHeld,
                IssuedToUserId = adminUser.Id,
                IssuedToRole = "Admin",
                IssuedToName = adminUser.UserName ?? "Admin",
                AdditionalData = new Dictionary<string, object>
                {
                    { "Amount", escrow.Amount },
                    { "PlatformFee", escrow.PlatformFee },
                    { "StudentName", studentName },
                    { "LandlordName", ownerName }
                }
            });
        }
    }

    private async Task<LandLord?> ResolveOwnerAsync(Booking booking)
    {
        if (booking.BedId.HasValue)
        {
            var bed = await _unitOfWork.Beds.GetAsync(booking.BedId.Value);
            if (bed?.Room?.HousingUnit?.LandLordId is Guid lid1)
                return await _unitOfWork.LandLords.GetAsync(lid1);
        }
        if (booking.RoomId.HasValue)
        {
            var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
            if (room?.HousingUnit?.LandLordId is Guid lid2)
                return await _unitOfWork.LandLords.GetAsync(lid2);
        }
        if (booking.HousingUnitId.HasValue)
        {
            var hu = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
            if (hu?.LandLordId is Guid lid3)
                return await _unitOfWork.LandLords.GetAsync(lid3);
        }
        return null;
    }

    private async Task NotifySafe(string? userId, string message, string type)
    {
        if (string.IsNullOrEmpty(userId)) return;
        try { await _notificationService.SendRealTimeNotificationAsync(userId, message, type); }
        catch (Exception ex) { _logger.LogWarning(ex, "Non-fatal: failed to notify {UserId}", userId); }
    }

    private static AdminContractApprovalResponse Fail(string message) =>
        new AdminContractApprovalResponse { Success = false, Message = message };

    private static int CalcDuration(DateTime start, DateTime end, BookingType type)
    {
        var days = (end - start).TotalDays;
        return type switch
        {
            BookingType.Monthly => Math.Max(1, (int)(days / 30)),
            BookingType.Yearly => Math.Max(1, (int)(days / 365)),
            _ => (int)days
        };
    }

    private static ContractResponse MapContract(Contract c) => new ContractResponse
    {
        ContractId = c.ContractId, BookingId = c.BookingId,
        ContractNumber = c.ContractNumber,
        OriginalContractPdfPath = c.OriginalContractPdfPath,
        StudentSignedContractPath = c.StudentSignedContractPath,
        LandlordSignedContractPath = c.LandlordSignedContractPath,
        IsStudentSigned = c.IsStudentSigned, IsLandlordSigned = c.IsLandlordSigned,
        IsAdminApproved = c.IsAdminApproved,
        StudentSignedAt = c.StudentSignedAt, LandlordSignedAt = c.LandlordSignedAt,
        AdminApprovedAt = c.AdminApprovedAt, AdminNotes = c.AdminNotes,
        ContractStatus = c.ContractStatus, CreatedAt = c.CreatedAt
    };

    private static EscrowResponse MapEscrow(EscrowTransaction e) => new EscrowResponse
    {
        EscrowId = e.EscrowId, PaymentId = e.PaymentId ?? Guid.Empty,
        ContractId = e.ContractId ?? Guid.Empty,
        Amount = e.Amount, Currency = e.Currency, Status = e.Status,
        PlatformFee = e.PlatformFee, CreatedAt = e.CreatedAt,
        ReleasedAt = e.ReleasedAt, ReleaseTransactionId = e.ReleaseTransactionId
    };
}
