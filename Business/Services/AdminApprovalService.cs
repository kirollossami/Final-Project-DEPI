using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace Business.Services;

public class AdminApprovalService : IAdminApprovalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEscrowService _escrowService;
    private readonly IReceiptService _receiptService;
    private readonly INotificationService _notificationService;
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly ILogger<AdminApprovalService> _logger;

    public AdminApprovalService(
        IUnitOfWork unitOfWork,
        IEscrowService escrowService,
        IReceiptService receiptService,
        INotificationService notificationService,
        IPaymentHistoryService paymentHistoryService,
        ILogger<AdminApprovalService> logger)
    {
        _unitOfWork = unitOfWork;
        _escrowService = escrowService;
        _receiptService = receiptService;
        _notificationService = notificationService;
        _paymentHistoryService = paymentHistoryService;
        _logger = logger;
    }

    public async Task<AdminContractApprovalResponse> ApproveContractAsync(AdminContractApprovalRequest request)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Step 1: Get contract
            var contract = await _unitOfWork.Contracts.GetAsync(request.ContractId);
            if (contract == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Contract not found"
                };
            }

            // Step 2: Validate contract is ready for approval
            if (!contract.IsStudentSigned || !contract.IsOwnerSigned)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Contract must be signed by both parties before approval"
                };
            }

            if (contract.IsAdminApproved)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Contract is already approved"
                };
            }

            // Step 3: Update contract approval status
            contract.IsAdminApproved = true;
            contract.AdminUserId = request.AdminUserId;
            contract.AdminApprovedAt = DateTime.UtcNow;
            contract.AdminNotes = request.AdminNotes;
            contract.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Contracts.Update(contract);

            // Step 4: Get booking and update status
            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);
            if (booking == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Booking not found"
                };
            }

            // Get payment for history tracking
            var payment = await _unitOfWork.Payments.GetAsync(booking.BookingId);

            // Update booking status - now it's awaiting escrow release
            var previousStatus = booking.BookingStatus.ToString();
            booking.BookingStatus = BookingStatus.AwaitingAdminApproval;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"Contract {contract.ContractNumber} approved by admin {request.AdminUserId}");

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
                    BookingStatus.AwaitingAdminApproval.ToString(),
                    request.AdminUserId,
                    "Admin",
                    metadata: new Dictionary<string, object>
                    {
                        { "ContractId", contract.ContractId },
                        { "ContractNumber", contract.ContractNumber },
                        { "AdminNotes", request.AdminNotes ?? "" }
                    });
            }

            // Step 5: Send notifications
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            if (student != null)
            {
                await _notificationService.SendRealTimeNotificationAsync(
                    student.UserId ?? string.Empty,
                    $"Your contract has been approved. The booking will be confirmed shortly.",
                    "ContractApproved");
            }

            // Get owner for notification
            LandLord? owner = null;
            if (booking.RoomId.HasValue)
            {
                var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                if (room != null)
                {
                    var housingUnit = await _unitOfWork.HousingUnits.GetAsync(room.HousingUnitId);
                    if (housingUnit != null)
                    {
                        owner = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
                    }
                }
            }
            else if (booking.HousingUnitId.HasValue)
            {
                var housingUnit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                if (housingUnit != null)
                {
                    owner = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
                }
            }

            if (owner != null)
            {
                await _notificationService.SendRealTimeNotificationAsync(
                    owner.UserId ?? string.Empty,
                    $"Contract for your property has been approved. Escrow funds are held and awaiting final booking confirmation.",
                    "ContractApproved");
            }

            return new AdminContractApprovalResponse
            {
                Success = true,
                Message = "Contract approved successfully",
                ContractId = contract.ContractId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving contract");
            await _unitOfWork.RollbackTransactionAsync();
            return new AdminContractApprovalResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<AdminContractApprovalResponse> RejectContractAsync(AdminContractApprovalRequest request)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Step 1: Get contract
            var contract = await _unitOfWork.Contracts.GetAsync(request.ContractId);
            if (contract == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Contract not found"
                };
            }

            // Step 2: Update contract with rejection notes
            contract.AdminUserId = request.AdminUserId;
            contract.AdminNotes = request.AdminNotes;
            contract.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Contracts.Update(contract);

            // Step 3: Get booking and update status
            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);
            if (booking == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Booking not found"
                };
            }

            var payment = await _unitOfWork.Payments.GetAsync(booking.BookingId);
            var previousStatus = booking.BookingStatus.ToString();

            booking.BookingStatus = BookingStatus.Rejected;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            // Step 4: Get escrow and refund if exists
            var escrow = await _unitOfWork.EscrowTransactions.GetByContractIdAsync(contract.ContractId);
            if (escrow != null && escrow.Status == EscrowStatus.Holding)
            {
                var refundRequest = new EscrowRefundRequest
                {
                    EscrowId = escrow.EscrowId,
                    AdminUserId = request.AdminUserId,
                    RefundReason = $"Contract rejected by admin: {request.AdminNotes}"
                };

                await _escrowService.RefundEscrowAsync(refundRequest);

                // Record refund event in payment history
                if (payment != null)
                {
                    var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                    await _paymentHistoryService.RecordPaymentEventAsync(
                        payment.PaymentId,
                        booking.BookingId,
                        escrow.EscrowId,
                        student?.UserId ?? string.Empty,
                        "EscrowRefunded",
                        $"Escrow refunded due to contract rejection: {request.AdminNotes}",
                        escrow.HeldAmount,
                        EscrowStatus.Holding.ToString(),
                        EscrowStatus.Refunded.ToString(),
                        request.AdminUserId,
                        "Admin",
                        metadata: new Dictionary<string, object>
                        {
                            { "RefundReason", refundRequest.RefundReason }
                        });
                }
            }

            // Record contract rejection in payment history
            if (payment != null)
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                await _paymentHistoryService.RecordPaymentEventAsync(
                    payment.PaymentId,
                    booking.BookingId,
                    null,
                    student?.UserId ?? string.Empty,
                    "ContractRejectedByAdmin",
                    $"Contract {contract.ContractNumber} rejected: {request.AdminNotes}",
                    payment.Amount,
                    previousStatus,
                    BookingStatus.Rejected.ToString(),
                    request.AdminUserId,
                    "Admin",
                    metadata: new Dictionary<string, object>
                    {
                        { "ContractNumber", contract.ContractNumber },
                        { "RejectionReason", request.AdminNotes ?? "" }
                    });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"Contract {contract.ContractNumber} rejected by admin {request.AdminUserId}");

            // Step 5: Send notifications
            var studentNotif = await _unitOfWork.Students.GetAsync(booking.StudentId);
            if (studentNotif != null)
            {
                await _notificationService.SendRealTimeNotificationAsync(
                    studentNotif.UserId ?? string.Empty,
                    $"Your contract has been rejected. Reason: {request.AdminNotes}. Your payment has been refunded.",
                    "ContractRejected");
            }

            return new AdminContractApprovalResponse
            {
                Success = true,
                Message = "Contract rejected and payment refunded successfully",
                ContractId = contract.ContractId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting contract");
            await _unitOfWork.RollbackTransactionAsync();
            return new AdminContractApprovalResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<AdminContractApprovalResponse> ProcessEscrowReleaseAsync(EscrowReleaseRequest request)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Step 1: Get escrow transaction
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Escrow transaction not found"
                };
            }

            // Step 2: Validate escrow is in holding status
            if (escrow.Status != EscrowStatus.Holding)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = $"Escrow is not in holding status. Current status: {escrow.Status}"
                };
            }

            // Step 3: Get contract and validate admin approval
            var contract = await _unitOfWork.Contracts.GetAsync(escrow.ContractId);
            if (contract == null || !contract.IsAdminApproved)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Contract must be admin-approved before escrow release"
                };
            }

            // Step 4: Release escrow
            var escrowResponse = await _escrowService.ReleaseEscrowAsync(request);
            if (escrowResponse.Status != EscrowStatus.Released)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Escrow release failed"
                };
            }

            // Step 5: Process owner payout
            var payoutResponse = await _escrowService.ProcessOwnerPayoutAsync(request.EscrowId);

            // Step 6: Get payment for history tracking
            var payment = await _unitOfWork.Payments.GetAsync(escrow.PaymentId);
            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);

            // Step 7: Record payment history - Escrow Released
            if (payment != null && booking != null)
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                await _paymentHistoryService.RecordPaymentEventAsync(
                    payment.PaymentId,
                    booking.BookingId,
                    escrow.EscrowId,
                    student?.UserId ?? string.Empty,
                    "EscrowReleased",
                    $"Escrow funds of {escrow.HeldAmount} EGP released after admin approval",
                    escrow.HeldAmount,
                    EscrowStatus.Holding.ToString(),
                    EscrowStatus.Released.ToString(),
                    request.AdminUserId,
                    "Admin",
                    metadata: new Dictionary<string, object>
                    {
                        { "ReleaseTransactionId", escrow.ReleaseTransactionId ?? "" },
                        { "ReleaseNotes", request.ReleaseNotes ?? "" }
                    });
            }

            // Step 8: Generate payout receipt for owner
            var owner = GetOwnerFromBooking(booking);
            if (owner != null && escrow.OwnerPayoutAmount.HasValue)
            {
                var payoutReceiptRequest = new ReceiptGenerationRequest
                {
                    PaymentId = escrow.PaymentId,
                    EscrowId = escrow.EscrowId,
                    Type = ReceiptType.OwnerPayout,
                    IssuedToUserId = owner.UserId ?? string.Empty,
                    IssuedToRole = "Owner",
                    IssuedToName = owner.User?.UserName ?? "Unknown",
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "PayoutAmount", escrow.OwnerPayoutAmount.Value },
                        { "PlatformFee", escrow.PlatformFee },
                        { "OriginalHeldAmount", escrow.HeldAmount },
                        { "PayoutTransactionId", escrow.OwnerPayoutTransactionId ?? "" }
                    }
                };

                await _receiptService.GenerateReceiptAsync(payoutReceiptRequest);

                // Record payment history - Owner Payout
                if (payment != null && booking != null)
                {
                    await _paymentHistoryService.RecordPaymentEventAsync(
                        payment.PaymentId,
                        booking.BookingId,
                        escrow.EscrowId,
                        owner.UserId ?? string.Empty,
                        "OwnerPayoutProcessed",
                        $"Owner payout of {escrow.OwnerPayoutAmount.Value} EGP processed",
                        escrow.OwnerPayoutAmount.Value,
                        EscrowStatus.Released.ToString(),
                        EscrowStatus.Released.ToString(),
                        request.AdminUserId,
                        "Admin",
                        metadata: new Dictionary<string, object>
                        {
                            { "OwnerPayoutTransactionId", escrow.OwnerPayoutTransactionId ?? "" },
                            { "PlatformFeePercentage", escrow.PlatformFeePercentage }
                        });
                }

                // Send notification to owner
                await _notificationService.SendRealTimeNotificationAsync(
                    owner.UserId ?? string.Empty,
                    $"Payout of {escrow.OwnerPayoutAmount.Value:N2} EGP has been processed for your booking.",
                    "PayoutProcessed");
            }

            // Step 9: Generate admin receipt
            var adminReceiptRequest = new ReceiptGenerationRequest
            {
                PaymentId = escrow.PaymentId,
                EscrowId = escrow.EscrowId,
                Type = ReceiptType.EscrowReleased,
                IssuedToUserId = request.AdminUserId,
                IssuedToRole = "Admin",
                IssuedToName = "System Admin",
                AdditionalData = new Dictionary<string, object>
                {
                    { "TotalHeldAmount", escrow.HeldAmount },
                    { "PlatformFee", escrow.PlatformFee },
                    { "OwnerPayoutAmount", escrow.OwnerPayoutAmount ?? 0 },
                    { "ReleaseApprovedBy", request.AdminUserId }
                }
            };

            await _receiptService.GenerateReceiptAsync(adminReceiptRequest);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"Escrow {request.EscrowId} released and payout processed for booking {booking?.BookingId}");

            // Step 10: Send notifications
            if (booking != null)
            {
                var studentNotif = await _unitOfWork.Students.GetAsync(booking.StudentId);
                if (studentNotif != null)
                {
                    await _notificationService.SendRealTimeNotificationAsync(
                        studentNotif.UserId ?? string.Empty,
                        $"Your booking has been confirmed! Payment received and secured.",
                        "BookingConfirmed");
                }
            }

            return new AdminContractApprovalResponse
            {
                Success = true,
                Message = "Escrow released and owner payout processed successfully",
                EscrowId = request.EscrowId,
                IsOwnerPayoutProcessed = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing escrow release");
            await _unitOfWork.RollbackTransactionAsync();
            return new AdminContractApprovalResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<AdminContractApprovalResponse> ProcessEscrowRefundAsync(EscrowRefundRequest request)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // Step 1: Get escrow
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Escrow transaction not found"
                };
            }

            // Step 2: Process refund
            var escrowResponse = await _escrowService.RefundEscrowAsync(request);
            if (escrowResponse.Status != EscrowStatus.Refunded)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return new AdminContractApprovalResponse
                {
                    Success = false,
                    Message = "Escrow refund failed"
                };
            }

            // Step 3: Get booking and update status
            var contract = await _unitOfWork.Contracts.GetAsync(escrow.ContractId);
            var booking = await _unitOfWork.Bookings.GetAsync(contract.BookingId);
            
            booking.BookingStatus = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            // Step 4: Get payment for history tracking
            var payment = await _unitOfWork.Payments.GetAsync(escrow.PaymentId);
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);

            // Generate refund receipt
            if (student != null)
            {
                var refundReceiptRequest = new ReceiptGenerationRequest
                {
                    PaymentId = escrow.PaymentId,
                    EscrowId = escrow.EscrowId,
                    Type = ReceiptType.RefundIssued,
                    IssuedToUserId = student.UserId ?? string.Empty,
                    IssuedToRole = "Student",
                    IssuedToName = student.User?.UserName ?? "Unknown",
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "RefundAmount", escrow.HeldAmount },
                        { "RefundReason", request.RefundReason },
                        { "RefundTransactionId", escrow.RefundTransactionId }
                    }
                };

                await _receiptService.GenerateReceiptAsync(refundReceiptRequest);

                // Record refund event in payment history
                if (payment != null)
                {
                    await _paymentHistoryService.RecordPaymentEventAsync(
                        payment.PaymentId,
                        booking.BookingId,
                        escrow.EscrowId,
                        student.UserId ?? string.Empty,
                        "EscrowManualRefund",
                        $"Escrow manually refunded by admin: {request.RefundReason}",
                        escrow.HeldAmount,
                        EscrowStatus.Holding.ToString(),
                        EscrowStatus.Refunded.ToString(),
                        request.AdminUserId,
                        "Admin",
                        metadata: new Dictionary<string, object>
                        {
                            { "RefundReason", request.RefundReason ?? "" },
                            { "RefundTransactionId", escrow.RefundTransactionId ?? "" }
                        });
                }

                // Send notification to student
                await _notificationService.SendRealTimeNotificationAsync(
                    student.UserId ?? string.Empty,
                    $"Refund of {escrow.HeldAmount:N2} EGP has been processed. Reason: {request.RefundReason}",
                    "RefundProcessed");
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation($"Escrow refund processed for escrow {request.EscrowId}");

            return new AdminContractApprovalResponse
            {
                Success = true,
                Message = "Escrow refund processed successfully",
                EscrowId = request.EscrowId,
                IsEscrowRefunded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing escrow refund");
            await _unitOfWork.RollbackTransactionAsync();
            return new AdminContractApprovalResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<ContractResponse>> GetPendingContractsAsync()
    {
        var contracts = await _unitOfWork.Contracts.GetAwaitingAdminApprovalAsync();
        return contracts.Select(c => new ContractResponse
        {
            ContractId = c.ContractId,
            ContractNumber = c.ContractNumber,
            GeneratedPdfUrl = c.GeneratedPdfUrl,
            IsStudentSigned = c.IsStudentSigned,
            IsOwnerSigned = c.IsOwnerSigned,
            IsAdminApproved = c.IsAdminApproved,
            CreatedAt = c.CreatedAt
        });
    }

    public async Task<IEnumerable<EscrowResponse>> GetPendingEscrowReleasesAsync()
    {
        var escrows = await _unitOfWork.EscrowTransactions.GetPendingReleaseAsync();
        return escrows.Select(e => new EscrowResponse
        {
            EscrowId = e.EscrowId,
            PaymentId = e.PaymentId,
            ContractId = e.ContractId,
            HeldAmount = e.HeldAmount,
            Currency = e.Currency,
            Status = e.Status,
            PlatformFee = e.PlatformFee,
            CreatedAt = e.CreatedAt,
            ReleasedAt = e.ReleasedAt,
            ReleaseTransactionId = e.ReleaseTransactionId
        });
    }

    private LandLord? GetOwnerFromBooking(Booking? booking)
    {
        if (booking == null) return null;

        if (booking.RoomId.HasValue && booking.Room != null)
        {
            var room = booking.Room;
            if (room.HousingUnit != null)
            {
                return room.HousingUnit.LandLord;
            }
        }
        else if (booking.HousingUnitId.HasValue && booking.HousingUnit != null)
        {
            return booking.HousingUnit.LandLord;
        }

        return null;
    }
}
