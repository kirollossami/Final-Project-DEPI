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
/// Handles admin approval/rejection of bookings after both parties have signed the contract.
/// Manages balance transfers between admin, student, and landlord accounts.
/// </summary>
public class BookingApprovalService : IBookingApprovalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBalanceService _balanceService;
    private readonly IEscrowService _escrowService;
    private readonly IReceiptService _receiptService;
    private readonly INotificationService _notificationService;
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<BookingApprovalService> _logger;

    public BookingApprovalService(
        IUnitOfWork unitOfWork,
        IBalanceService balanceService,
        IEscrowService escrowService,
        IReceiptService receiptService,
        INotificationService notificationService,
        IPaymentHistoryService paymentHistoryService,
        UserManager<User> userManager,
        ILogger<BookingApprovalService> logger)
    {
        _unitOfWork = unitOfWork;
        _balanceService = balanceService;
        _escrowService = escrowService;
        _receiptService = receiptService;
        _notificationService = notificationService;
        _paymentHistoryService = paymentHistoryService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<BookingApprovalResponse> ApproveBookingAsync(Guid bookingId, string adminUserId, string? adminNotes = null)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Approving booking {BookingId} by admin {AdminUserId}", bookingId, adminUserId);

            // 1. Load booking and validate state
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                throw new ArgumentException($"Booking {bookingId} not found");

            if (booking.BookingStatus != BookingStatus.WaitingForAdminApproval)
                throw new InvalidOperationException($"Booking must be in WaitingForAdminApproval status. Current: {booking.BookingStatus}");

            // 2. Load contract and validate signatures
            var contract = await _unitOfWork.Contracts.GetByBookingIdAsync(bookingId);
            if (contract == null)
                throw new ArgumentException($"Contract not found for booking {bookingId}");

            if (!contract.IsStudentSigned || !contract.IsLandlordSigned)
                throw new InvalidOperationException("Both student and landlord must sign the contract before approval");

            // 3. Load payment and escrow
            var payment = await _unitOfWork.Payments.GetByBookingIdAsync(bookingId);
            if (payment == null)
                throw new ArgumentException($"Payment not found for booking {bookingId}");

            var escrow = await _unitOfWork.EscrowTransactions.GetByPaymentIdAsync(payment.PaymentId);
            if (escrow == null)
                throw new ArgumentException($"Escrow not found for payment {payment.PaymentId}");

            // 4. Load landlord
            LandLord? landlord = null;
            if (booking.RoomId.HasValue)
            {
                var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                if (room != null)
                {
                    var housingUnit = await _unitOfWork.HousingUnits.GetAsync(room.HousingUnitId);
                    if (housingUnit != null)
                        landlord = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
                }
            }
            else if (booking.HousingUnitId.HasValue)
            {
                var housingUnit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                if (housingUnit != null)
                    landlord = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
            }

            if (landlord == null || string.IsNullOrEmpty(landlord.UserId))
                throw new ArgumentException("Landlord not found or has no user ID");

            // 5. Load student
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            if (student == null || string.IsNullOrEmpty(student.UserId))
                throw new ArgumentException("Student not found or has no user ID");

            // 6. Calculate payout amount (payment minus platform fee)
            var payoutAmount = escrow.Amount - escrow.PlatformFee;

            // 7. Transfer from admin balance to landlord balance
            var adminUser = await _userManager.FindByIdAsync(adminUserId);
            if (adminUser == null)
                throw new ArgumentException("Admin user not found");

            await _balanceService.TransferBalanceAsync(
                adminUser.Id,
                landlord.UserId,
                "LandLord",
                payoutAmount,
                $"Booking {bookingId} approved - landlord payout");

            _logger.LogInformation("Transferred {Amount} EGP from admin to landlord for booking {BookingId}",
                payoutAmount, bookingId);

            // 8. Release escrow
            var releaseRequest = new EscrowReleaseRequest
            {
                EscrowId = escrow.EscrowId,
                AdminUserId = adminUserId,
                ReleaseNotes = adminNotes ?? "Booking approved by admin"
            };
            await _escrowService.ReleaseEscrowAsync(releaseRequest);

            // 9. Process landlord payout through escrow service
            await _escrowService.ProcessOwnerPayoutAsync(escrow.EscrowId);

            // 10. Update contract approval status
            contract.IsAdminApproved = true;
            contract.AdminUserId = adminUserId;
            contract.AdminApprovedAt = DateTime.UtcNow;
            contract.AdminNotes = adminNotes;
            contract.ContractStatus = ContractStatus.Approved;
            contract.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Contracts.Update(contract);

            // 11. Update booking status
            booking.BookingStatus = BookingStatus.Approved;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            // 12. Generate payout receipt for landlord
            var landlordUser = await _userManager.FindByIdAsync(landlord.UserId);
            var landlordReceiptReq = new ReceiptGenerationRequest
            {
                PaymentId = payment.PaymentId,
                EscrowId = escrow.EscrowId,
                Type = ReceiptType.OwnerPayout,
                IssuedToUserId = landlord.UserId,
                IssuedToRole = "LandLord",
                IssuedToName = landlordUser?.UserName ?? landlord.User?.UserName ?? "Unknown",
                AdditionalData = new Dictionary<string, object>
                {
                    { "BookingId", bookingId },
                    { "ContractId", contract.ContractId },
                    { "PayoutAmount", payoutAmount },
                    { "PlatformFee", escrow.PlatformFee },
                    { "ApprovedBy", adminUser.UserName ?? "Admin" }
                }
            };
            await _receiptService.GenerateEscrowReceiptAsync(landlordReceiptReq);
            _logger.LogInformation("Landlord payout receipt generated");

            // 13. Record payment history
            await _paymentHistoryService.RecordPaymentEventAsync(
                payment.PaymentId,
                bookingId,
                escrow.EscrowId,
                landlord.UserId,
                "BookingApproved",
                $"Booking approved. Payout of {payoutAmount} EGP transferred to landlord. Platform fee: {escrow.PlatformFee} EGP.",
                payoutAmount,
                BookingStatus.WaitingForAdminApproval.ToString(),
                BookingStatus.Approved.ToString(),
                adminUserId,
                adminUser.UserName ?? "Admin",
                metadata: new Dictionary<string, object>
                {
                    { "ContractId", contract.ContractId },
                    { "EscrowId", escrow.EscrowId },
                    { "PlatformFee", escrow.PlatformFee }
                });

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // 14. Send notifications
            await NotifySafe(landlord.UserId,
                $"Your booking has been approved! Payout of {payoutAmount:N2} EGP has been transferred to your account.",
                "BookingApproved");

            await NotifySafe(student.UserId,
                $"The booking has been approved by the admin.",
                "BookingApproved");

            _logger.LogInformation("Booking {BookingId} approved successfully", bookingId);

            return new BookingApprovalResponse
            {
                Success = true,
                Message = "Booking approved successfully. Funds transferred to landlord.",
                BookingId = bookingId,
                EscrowId = escrow.EscrowId,
                AmountTransferred = payoutAmount,
                Currency = "EGP",
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving booking {BookingId}", bookingId);
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<BookingApprovalResponse> RejectBookingAsync(Guid bookingId, string adminUserId, string rejectionReason)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Rejecting booking {BookingId} by admin {AdminUserId}. Reason: {Reason}",
                bookingId, adminUserId, rejectionReason);

            // 1. Load booking and validate state
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                throw new ArgumentException($"Booking {bookingId} not found");

            if (booking.BookingStatus != BookingStatus.WaitingForAdminApproval)
                throw new InvalidOperationException($"Booking must be in WaitingForAdminApproval status. Current: {booking.BookingStatus}");

            // 2. Load contract
            var contract = await _unitOfWork.Contracts.GetByBookingIdAsync(bookingId);
            if (contract == null)
                throw new ArgumentException($"Contract not found for booking {bookingId}");

            // 3. Load payment and escrow
            var payment = await _unitOfWork.Payments.GetByBookingIdAsync(bookingId);
            if (payment == null)
                throw new ArgumentException($"Payment not found for booking {bookingId}");

            var escrow = await _unitOfWork.EscrowTransactions.GetByPaymentIdAsync(payment.PaymentId);
            if (escrow == null)
                throw new ArgumentException($"Escrow not found for payment {payment.PaymentId}");

            // 4. Load student
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            if (student == null || string.IsNullOrEmpty(student.UserId))
                throw new ArgumentException("Student not found or has no user ID");

            // 5. Load admin
            var adminUser = await _userManager.FindByIdAsync(adminUserId);
            if (adminUser == null)
                throw new ArgumentException("Admin user not found");

            // 6. Refund from admin balance to student balance
            await _balanceService.TransferBalanceAsync(
                adminUser.Id,
                student.UserId,
                "Student",
                payment.Amount,
                $"Booking {bookingId} rejected - full refund");

            _logger.LogInformation("Refunded {Amount} EGP from admin to student for booking {BookingId}",
                payment.Amount, bookingId);

            // 7. Refund escrow
            var refundRequest = new EscrowRefundRequest
            {
                EscrowId = escrow.EscrowId,
                AdminUserId = adminUserId,
                RefundReason = rejectionReason
            };
            await _escrowService.RefundEscrowAsync(refundRequest);

            // 8. Update contract status
            contract.ContractStatus = ContractStatus.Rejected;
            contract.AdminUserId = adminUserId;
            contract.AdminNotes = rejectionReason;
            contract.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Contracts.Update(contract);

            // 9. Update booking status
            booking.BookingStatus = BookingStatus.Rejected;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            // 10. Generate refund receipt for student
            var studentUser = await _userManager.FindByIdAsync(student.UserId);
            var refundReceiptReq = new ReceiptGenerationRequest
            {
                PaymentId = payment.PaymentId,
                EscrowId = escrow.EscrowId,
                Type = ReceiptType.RefundIssued,
                IssuedToUserId = student.UserId,
                IssuedToRole = "Student",
                IssuedToName = studentUser?.UserName ?? student.User?.UserName ?? "Unknown",
                AdditionalData = new Dictionary<string, object>
                {
                    { "BookingId", bookingId },
                    { "ContractId", contract.ContractId },
                    { "RefundAmount", payment.Amount },
                    { "RejectionReason", rejectionReason },
                    { "RejectedBy", adminUser.UserName ?? "Admin" }
                }
            };
            await _receiptService.GenerateEscrowReceiptAsync(refundReceiptReq);
            _logger.LogInformation("Student refund receipt generated");

            // 11. Record payment history
            await _paymentHistoryService.RecordPaymentEventAsync(
                payment.PaymentId,
                bookingId,
                escrow.EscrowId,
                student.UserId,
                "BookingRejected",
                $"Booking rejected. Full refund of {payment.Amount} EGP processed. Reason: {rejectionReason}",
                payment.Amount,
                BookingStatus.WaitingForAdminApproval.ToString(),
                BookingStatus.Rejected.ToString(),
                adminUserId,
                adminUser.UserName ?? "Admin",
                metadata: new Dictionary<string, object>
                {
                    { "ContractId", contract.ContractId },
                    { "EscrowId", escrow.EscrowId },
                    { "RejectionReason", rejectionReason }
                });

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // 12. Send notifications
            await NotifySafe(student.UserId,
                $"Your booking has been rejected by the admin. Reason: {rejectionReason}. Full refund of {payment.Amount:N2} EGP has been processed.",
                "BookingRejected");

            // Notify landlord if exists
            LandLord? landlord = null;
            if (booking.RoomId.HasValue)
            {
                var room = await _unitOfWork.Rooms.GetAsync(booking.RoomId.Value);
                if (room != null)
                {
                    var housingUnit = await _unitOfWork.HousingUnits.GetAsync(room.HousingUnitId);
                    if (housingUnit != null)
                        landlord = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
                }
            }
            else if (booking.HousingUnitId.HasValue)
            {
                var housingUnit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
                if (housingUnit != null)
                    landlord = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
            }

            if (landlord != null && !string.IsNullOrEmpty(landlord.UserId))
            {
                await NotifySafe(landlord.UserId,
                    $"The booking has been rejected by the admin. Reason: {rejectionReason}.",
                    "BookingRejected");
            }

            _logger.LogInformation("Booking {BookingId} rejected successfully", bookingId);

            return new BookingApprovalResponse
            {
                Success = true,
                Message = "Booking rejected successfully. Full refund processed.",
                BookingId = bookingId,
                EscrowId = escrow.EscrowId,
                AmountTransferred = payment.Amount,
                Currency = "EGP",
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting booking {BookingId}", bookingId);
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task NotifySafe(string? userId, string message, string type)
    {
        if (string.IsNullOrEmpty(userId)) return;
        try
        {
            await _notificationService.SendRealTimeNotificationAsync(userId, message, type);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Non-fatal: failed to notify user {UserId}", userId);
        }
    }
}
