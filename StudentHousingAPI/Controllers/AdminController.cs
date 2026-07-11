using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly IAdminService _adminService;
    private readonly IStudentService _studentService;
    private readonly IComplaintService _complaintService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILandLordService _landLordService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IBalanceService _balanceService;
    private readonly IBookingPaymentService _bookingPaymentService;

    public AdminController(
        IAdminService adminService,
        IStudentService studentService,
        IComplaintService complaintService,
        IFileStorageService fileStorageService,
        ILandLordService landLordService,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IBalanceService balanceService,
        IBookingPaymentService bookingPaymentService)
    {
        _adminService = adminService;
        _studentService = studentService;
        _complaintService = complaintService;
        _fileStorageService = fileStorageService;
        _landLordService = landLordService;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _balanceService = balanceService;
        _bookingPaymentService = bookingPaymentService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] AdminUserFilterRequest filter)
    {
        var result = await _adminService.GetAllUsersAsync(filter);
        return Ok(result);
    }

    [HttpPost("users/{userId}/toggle-active")]
    public async Task<IActionResult> ToggleUserActiveStatus(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId == "undefined" || userId == "null")
            return BadRequest(new { Success = false, Message = "Invalid user ID." });
        var result = await _adminService.ToggleUserActiveStatusAsync(userId);
        if (!result.Success)
            return BadRequest(result);
        try { await _notificationService.SendRealTimeNotificationAsync(userId, "Your account status has been updated by admin.", NotificationTypes.AccountStatusChanged); } catch { }
        return Ok(result);
    }

    [HttpGet("verifications/pending")]
    public async Task<IActionResult> GetPendingVerifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _adminService.GetPendingVerificationsAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpPost("verifications/{studentId}/review")]
    public async Task<IActionResult> ReviewUniversityVerification(
        Guid studentId,
        [FromBody] ReviewVerificationRequest request)
    {
        var result = await _adminService.ReviewUniversityVerificationAsync(studentId, request.NewStatus);
        if (result == null)
            return BadRequest(new { Message = "Verification can only be reviewed when status is Pending, and status can only be set to Approved or Rejected." });
        try
        {
            var student = await _studentService.GetStudentByIdAsync(studentId);
            if (student?.UserId != null)
                await _notificationService.SendRealTimeNotificationAsync(student.UserId, $"Your university verification has been {request.NewStatus}.", NotificationTypes.VerificationReviewed);
        }
        catch { }
        return Ok(result);
    }

    [HttpGet("verifications/{studentId}/id-card")]
    public async Task<IActionResult> GetUniversityIdCard(Guid studentId)
    {
        var student = await _studentService.GetStudentByIdAsync(studentId);
        if (student == null)
            return NotFound(new { Message = "Student not found." });

        var idCardPath = await _studentService.GetUniversityIdCardPathAsync(studentId);
        if (string.IsNullOrEmpty(idCardPath))
            return NotFound(new { Message = "No ID card uploaded." });

        var file = await _fileStorageService.GetFileAsync(idCardPath);
        if (file == null)
            return NotFound(new { Message = "ID card file not found on disk." });

        return File(file.Value.Content, file.Value.ContentType);
    }

    [HttpGet("complaints")]
    public async Task<IActionResult> GetAllComplaints([FromQuery] ComplaintFilterRequest filter)
    {
        var result = await _complaintService.GetComplaintsAsync(filter);
        return Ok(result);
    }

    [HttpPut("complaints/{complaintId}/status")]
    public async Task<IActionResult> UpdateComplaintStatus(
        Guid complaintId,
        [FromBody] ComplaintUpdateRequest request)
    {
        request.ComplaintId = complaintId;
        var result = await _complaintService.UpdateComplaintAsync(request);
        if (result == null)
            return NotFound(new { Message = "Complaint not found." });
        try
        {
            var complaint = await _complaintService.GetComplaintByIdAsync(complaintId);
            if (complaint?.StudentId != null)
            {
                var student = await _studentService.GetStudentByIdAsync(complaint.StudentId);
                if (student?.UserId != null)
                    await _notificationService.SendRealTimeNotificationAsync(student.UserId, $"Your complaint status has been updated to {request.Status}.", NotificationTypes.ComplaintStatusUpdated);
            }
        }
        catch { }
        return Ok(result);
    }

    [HttpGet("commissions/report")]
    public async Task<IActionResult> GetCommissionReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _adminService.GetCommissionReportAsync(from, to);
        return Ok(result);
    }

    [HttpPut("landlords/{landlordId}/verification-status")]
    public async Task<IActionResult> UpdateLandlordVerificationStatus(
        Guid landlordId,
        [FromBody] UpdateLandlordVerificationStatusRequest request)
    {
        var result = await _adminService.UpdateLandlordVerificationStatusAsync(landlordId, request.Status);
        if (!result.Success)
            return BadRequest(result);
        try
        {
            var landlord = await _landLordService.GetLandLordByIdAsync(landlordId);
            if (landlord?.UserId != null)
                await _notificationService.SendRealTimeNotificationAsync(landlord.UserId, $"Your account verification status has been updated to {request.Status}.", NotificationTypes.VerificationStatusChanged);
        }
        catch { }
        return Ok(result);
    }

    [HttpGet("landlords/pending")]
    public async Task<IActionResult> GetPendingLandlords(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _adminService.GetPendingLandlordsAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("verification/{landlordId}/National-ID")]
    public async Task<IActionResult> GetLandlordNationalId(Guid landlordId)
    {
        var landlord = await _landLordService.GetLandLordByIdAsync(landlordId);
        if (landlord == null)
            return NotFound(new { Message = "Landlord not found." });

        if (string.IsNullOrEmpty(landlord.NationalIdImageUrl))
            return NotFound(new { Message = "No National ID document uploaded." });

        var file = await _fileStorageService.GetFileAsync(landlord.NationalIdImageUrl);
        if (file == null)
            return NotFound(new { Message = "National ID document file not found." });

        return File(file.Value.Content, file.Value.ContentType);
    }

    [HttpGet("verification/{landlordId}/Unit-Documentation")]
    public async Task<IActionResult> GetLandlordUnitDocumentation(Guid landlordId)
    {
        var landlord = await _landLordService.GetLandLordByIdAsync(landlordId);
        if (landlord == null)
            return NotFound(new { Message = "Landlord not found." });

        if (string.IsNullOrEmpty(landlord.HousingUnitDocumentationUrl))
            return NotFound(new { Message = "No Housing Unit Documentation uploaded." });

        var file = await _fileStorageService.GetFileAsync(landlord.HousingUnitDocumentationUrl);
        if (file == null)
            return NotFound(new { Message = "Housing Unit Documentation file not found." });

        return File(file.Value.Content, file.Value.ContentType);
    }

    [HttpGet("generate-password-hash")]
    [AllowAnonymous]
    public IActionResult GeneratePasswordHash([FromQuery] string password)
    {
        if (string.IsNullOrEmpty(password))
            return BadRequest(new { Message = "Password is required" });

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Domain.Entities.User>();
        var user = new Domain.Entities.User { UserName = "temp" };
        var hash = hasher.HashPassword(user, password);
        return Ok(new { Hash = hash });
    }

    /// <summary>
    /// Upload contract for a booking (manual upload workflow)
    /// </summary>
    [HttpPost("bookings/{bookingId}/upload-contract")]
    public async Task<IActionResult> UploadContract(Guid bookingId, IFormFile contractFile)
    {
        try
        {
            // Validate booking exists
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                return NotFound(new { Message = "Booking not found" });

            var payment = await _unitOfWork.Payments.GetByBookingIdAsync(bookingId);
            bool isPaymentCompleted = payment != null && payment.PaymentStatus == PaymentStatus.Completed;

            // ── Auto-recovery: if payment is complete but booking is still PendingPayment,
            //    the Paymob callback was missed. Fix the statuses now so the upload can proceed.
            if (booking.BookingStatus == BookingStatus.PendingPayment && isPaymentCompleted)
            {
                booking.BookingStatus = BookingStatus.WaitingForContract;
                booking.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Bookings.Update(booking);
                await _unitOfWork.SaveChangesAsync();
            }

            // ── Also handle: payment exists but is still Pending/Unknown status on the gateway.
            //    Admin is explicitly uploading a contract, which implies payment was received.
            //    Check if a completed PaymentTransaction exists even if Payment entity wasn't updated.
            if (!isPaymentCompleted && payment != null)
            {
                var paymentTxn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(payment.PaymentId);
                if (paymentTxn != null && paymentTxn.GatewayStatus == PaymentGatewayStatus.Success)
                {
                    // Gateway confirmed success but callback didn't update Payment entity — fix it
                    payment.PaymentStatus = PaymentStatus.Completed;
                    payment.CompletedAt = paymentTxn.CompletedAt ?? DateTime.UtcNow;
                    payment.TransactionId = paymentTxn.PaymobTransactionId ?? paymentTxn.PaymobOrderId;
                    await _unitOfWork.Payments.Update(payment);

                    booking.BookingStatus = BookingStatus.WaitingForContract;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Bookings.Update(booking);
                    await _unitOfWork.SaveChangesAsync();
                    isPaymentCompleted = true;
                }
            }

            if (booking.BookingStatus != BookingStatus.WaitingForContract)
                return BadRequest(new { Message = $"Booking is not ready for contract. Current status: {booking.BookingStatus}. Payment status: {payment?.PaymentStatus}" });

            if (contractFile == null || contractFile.Length == 0)
                return BadRequest(new { Message = "Contract file is required" });

            if (!contractFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Only PDF files are allowed" });

            // Upload contract file
            var fileName = $"contract_{bookingId}_{Guid.NewGuid()}.pdf";
            var filePath = await _fileStorageService.SaveFileAsync(contractFile.OpenReadStream(), fileName, "contracts");

            // Get booking details for contract
            var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
            var landlord = GetOwnerFromBooking(booking);

            // Create contract record
            var contract = new Contract
            {
                ContractId = Guid.NewGuid(),
                BookingId = bookingId,
                ContractNumber = $"CNT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                ReceivingDate = booking.StartDate,
                HandoverDate = booking.EndDate,
                FinalPrice = booking.TotalPrice,
                DurationType = booking.BookingType == BookingType.Monthly ? ContractDurationType.Monthly : ContractDurationType.Yearly,
                DurationValue = CalculateDuration(booking.StartDate, booking.EndDate, booking.BookingType),
                OwnerFullName = landlord?.User?.UserName ?? "Unknown",
                OwnerNationalId = landlord?.NationalId ?? "N/A",
                StudentFullName = student?.User?.UserName ?? "Unknown",
                StudentNationalId = student?.NationalId ?? "N/A",
                OriginalContractPdfPath = filePath,
                ContractStatus = ContractStatus.WaitingForSignatures,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Contracts.Insert(contract);
            
            // Update booking status
            booking.BookingStatus = BookingStatus.WaitingForSignatures;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            await _unitOfWork.SaveChangesAsync();

            // Notify student and landlord
            try
            {
                if (!string.IsNullOrEmpty(student?.UserId))
                    await _notificationService.SendRealTimeNotificationAsync(
                        student.UserId,
                        "Contract has been uploaded for your booking. Please review and sign the contract.",
                        NotificationTypes.ContractUploaded);

                if (!string.IsNullOrEmpty(landlord?.UserId))
                    await _notificationService.SendRealTimeNotificationAsync(
                        landlord.UserId,
                        "Contract has been uploaded for your booking. Please review and sign the contract.",
                        NotificationTypes.ContractUploaded);
            }
            catch (Exception ex)
            {
                // Log but don't fail the operation
                Console.Error.WriteLine($"Failed to send notifications: {ex.Message}");
            }

            return Ok(new
            {
                Message = "Contract uploaded successfully",
                ContractId = contract.ContractId,
                ContractNumber = contract.ContractNumber,
                FilePath = filePath
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error uploading contract: {ex.Message}" });
        }
    }

    /// <summary>
    /// Force-complete a stuck booking where Paymob payment succeeded
    /// but the callback was never processed (booking stuck in PendingPayment).
    /// This runs the FULL payment callback pipeline: receipts, escrow, balance, notifications.
    /// Admin-only recovery endpoint.
    /// </summary>
    [HttpPost("bookings/{bookingId}/force-complete-payment")]
    public async Task<IActionResult> ForceCompletePayment(Guid bookingId)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                return NotFound(new { Message = "Booking not found" });

            var payment = await _unitOfWork.Payments.GetByBookingIdAsync(bookingId);
            if (payment == null)
                return NotFound(new { Message = "No payment record found for this booking" });

            // Already completed — just verify state
            if (payment.PaymentStatus == PaymentStatus.Completed
                && booking.BookingStatus != BookingStatus.PendingPayment)
                return Ok(new { Message = "Payment already completed", PaymentStatus = payment.PaymentStatus.ToString(), BookingStatus = booking.BookingStatus.ToString() });

            var paymentTxn = await _unitOfWork.PaymentTransactions.GetByPaymentIdAsync(payment.PaymentId);
            if (paymentTxn == null)
                return BadRequest(new { Message = "No PaymentTransaction record found. Cannot force-complete without an initiation record." });

            // Force gateway status to Success — admin is explicitly authorizing this
            paymentTxn.GatewayStatus = PaymentGatewayStatus.Success;
            paymentTxn.CallbackProcessedAt = DateTime.UtcNow;
            paymentTxn.CompletedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(paymentTxn.PaymobTransactionId))
                paymentTxn.PaymobTransactionId = paymentTxn.PaymobOrderId; // fallback
            await _unitOfWork.PaymentTransactions.Update(paymentTxn);
            await _unitOfWork.SaveChangesAsync();

            // Run the full payment callback pipeline
            var result = await _bookingPaymentService.CompleteBookingWorkflowAsync(payment.PaymentId);

            if (!result.Success)
                return BadRequest(new { Message = $"Force-complete failed: {result.Message}" });

            try
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                if (student?.UserId != null)
                    await _notificationService.SendRealTimeNotificationAsync(student.UserId, "Your payment has been completed successfully.", NotificationTypes.PaymentCompleted);
                var landlord = GetOwnerFromBooking(booking);
                if (landlord?.UserId != null)
                    await _notificationService.SendRealTimeNotificationAsync(landlord.UserId, "Payment has been completed for your property.", NotificationTypes.PaymentCompleted);
            }
            catch { }

            return Ok(new
            {
                Message = "Payment force-completed successfully. Receipts generated, escrow created, balances credited.",
                BookingId = bookingId,
                PaymentId = payment.PaymentId,
                Result = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error force-completing payment: {ex.Message}", Details = ex.ToString() });
        }
    }

    /// <summary>
    /// Approve booking - release escrow to landlord
    /// </summary>
    [HttpPost("bookings/{bookingId}/approve")]
    public async Task<IActionResult> ApproveBooking(Guid bookingId, [FromBody] string? notes = null)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                return NotFound(new { Message = "Booking not found" });

            if (booking.BookingStatus != BookingStatus.WaitingForAdminApproval)
                return BadRequest(new { Message = $"Booking is not in WaitingForAdminApproval status. Current status: {booking.BookingStatus}" });

            var contract = await _unitOfWork.Contracts.GetByBookingIdAsync(bookingId);
            if (contract == null)
                return NotFound(new { Message = "Contract not found" });

            var escrow = await _unitOfWork.EscrowTransactions.GetByBookingIdAsync(bookingId);
            if (escrow == null)
                return NotFound(new { Message = "Escrow transaction not found" });

            var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Update contract status
            contract.ContractStatus = ContractStatus.Approved;
            contract.IsAdminApproved = true;
            contract.AdminUserId = adminUserId;
            contract.AdminApprovedAt = DateTime.UtcNow;
            contract.AdminNotes = notes;
            contract.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Contracts.Update(contract);

            // Release escrow to landlord
            escrow.Status = EscrowStatus.Released;
            escrow.ReleasedAt = DateTime.UtcNow;
            escrow.ReleasedByUserId = adminUserId;
            escrow.ReleaseTransactionId = $"RELEASE-{Guid.NewGuid()}";
            escrow.ReleaseNotes = notes;
            escrow.LandlordPayoutAmount = escrow.Amount - escrow.PlatformFee;
            escrow.LandlordPayoutAt = DateTime.UtcNow;
            escrow.LandlordPayoutTransactionId = $"PAYOUT-{Guid.NewGuid()}";
            escrow.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.EscrowTransactions.Update(escrow);

            // Update booking status
            booking.BookingStatus = BookingStatus.Approved;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            await _unitOfWork.SaveChangesAsync();

            // Add balance to landlord using balance service
            try
            {
                var landlord = GetOwnerFromBooking(booking);
                if (landlord != null && !string.IsNullOrEmpty(landlord.UserId))
                {
                    await _balanceService.AddToBalanceAsync(
                        landlord.UserId,
                        "LandLord",
                        escrow.LandlordPayoutAmount ?? 0,
                        $"Escrow release for booking {bookingId}"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to add balance to landlord: {ex.Message}");
            }

            // Notify student and landlord
            try
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                var landlord = GetOwnerFromBooking(booking);

                if (!string.IsNullOrEmpty(student?.UserId))
                    await _notificationService.SendRealTimeNotificationAsync(
                        student.UserId,
                        "Your booking has been approved. The landlord has received the payment.",
                        NotificationTypes.BookingApproved);

                if (!string.IsNullOrEmpty(landlord?.UserId))
                    await _notificationService.SendRealTimeNotificationAsync(
                        landlord.UserId,
                        "Booking has been approved. Payment has been released to your account.",
                        NotificationTypes.BookingApproved);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to send notifications: {ex.Message}");
            }

            return Ok(new
            {
                Message = "Booking approved successfully",
                BookingId = bookingId,
                EscrowReleased = true,
                AmountReleased = escrow.LandlordPayoutAmount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error approving booking: {ex.Message}" });
        }
    }

    /// <summary>
    /// Reject booking - refund escrow to student
    /// </summary>
    [HttpPost("bookings/{bookingId}/reject")]
    public async Task<IActionResult> RejectBooking(Guid bookingId, [FromBody] string reason)
    {
        try
        {
            if (string.IsNullOrEmpty(reason))
                return BadRequest(new { Message = "Rejection reason is required" });

            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking == null)
                return NotFound(new { Message = "Booking not found" });

            if (booking.BookingStatus != BookingStatus.WaitingForAdminApproval)
                return BadRequest(new { Message = $"Booking is not in WaitingForAdminApproval status. Current status: {booking.BookingStatus}" });

            var contract = await _unitOfWork.Contracts.GetByBookingIdAsync(bookingId);
            if (contract == null)
                return NotFound(new { Message = "Contract not found" });

            var escrow = await _unitOfWork.EscrowTransactions.GetByBookingIdAsync(bookingId);
            if (escrow == null)
                return NotFound(new { Message = "Escrow transaction not found" });

            var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Update contract status
            contract.ContractStatus = ContractStatus.Rejected;
            contract.IsAdminApproved = false;
            contract.AdminUserId = adminUserId;
            contract.AdminApprovedAt = DateTime.UtcNow;
            contract.AdminNotes = reason;
            contract.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Contracts.Update(contract);

            // Refund escrow to student
            escrow.Status = EscrowStatus.Refunded;
            escrow.RefundedAt = DateTime.UtcNow;
            escrow.RefundTransactionId = $"REFUND-{Guid.NewGuid()}";
            escrow.RefundReason = reason;
            escrow.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.EscrowTransactions.Update(escrow);

            // Update booking status
            booking.BookingStatus = BookingStatus.Rejected;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.Update(booking);

            await _unitOfWork.SaveChangesAsync();

            // Add balance to student (refund)
            try
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                if (student != null && !string.IsNullOrEmpty(student.UserId))
                {
                    await _balanceService.AddToBalanceAsync(
                        student.UserId,
                        "Student",
                        escrow.Amount,
                        $"Escrow refund for rejected booking {bookingId}"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to add balance to student: {ex.Message}");
            }

            // Notify student and landlord
            try
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                var landlord = GetOwnerFromBooking(booking);

                if (!string.IsNullOrEmpty(student?.UserId))
                    await _notificationService.SendRealTimeNotificationAsync(
                        student.UserId,
                        $"Your booking has been rejected. Reason: {reason}. Your payment has been refunded.",
                        NotificationTypes.BookingRejected);

                if (!string.IsNullOrEmpty(landlord?.UserId))
                    await _notificationService.SendRealTimeNotificationAsync(
                        landlord.UserId,
                        $"Booking has been rejected. Reason: {reason}.",
                        NotificationTypes.BookingRejected);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to send notifications: {ex.Message}");
            }

            return Ok(new
            {
                Message = "Booking rejected successfully",
                BookingId = bookingId,
                RefundProcessed = true,
                RefundAmount = escrow.Amount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = $"Error rejecting booking: {ex.Message}" });
        }
    }

    private LandLord? GetOwnerFromBooking(Booking? booking)
    {
        if (booking == null) return null;

        if (booking.BedId.HasValue)
        {
            var bed = _unitOfWork.Beds.GetAsync(booking.BedId.Value).Result;
            if (bed?.Room?.HousingUnit != null)
                return _unitOfWork.LandLords.GetAsync(bed.Room.HousingUnit.LandLordId).Result;
        }
        else if (booking.RoomId.HasValue)
        {
            var room = _unitOfWork.Rooms.GetAsync(booking.RoomId.Value).Result;
            if (room?.HousingUnit != null)
                return _unitOfWork.LandLords.GetAsync(room.HousingUnit.LandLordId).Result;
        }
        else if (booking.HousingUnitId.HasValue)
        {
            var housingUnit = _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value).Result;
            if (housingUnit != null)
                return _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId).Result;
        }

        return null;
    }

    private int CalculateDuration(DateTime startDate, DateTime endDate, BookingType bookingType)
    {
        var duration = endDate - startDate;
        return bookingType switch
        {
            BookingType.Monthly => (int)(duration.TotalDays / 30),
            BookingType.Yearly => (int)(duration.TotalDays / 365),
            _ => (int)duration.TotalDays
        };
    }
}
