using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminApprovalController : BaseController
{
    private readonly IAdminApprovalService _adminApprovalService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public AdminApprovalController(
        IAdminApprovalService adminApprovalService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _adminApprovalService = adminApprovalService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("approve-booking/{bookingId}")]
    public async Task<IActionResult> ApproveBooking(Guid bookingId, [FromBody] AdminContractApprovalRequest? request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized(new { Message = "Admin user not identified" });

        var success = await HttpContext.RequestServices.GetRequiredService<IBookingService>()
            .ApproveBookingAsync(bookingId, adminUserId, request?.AdminNotes);

        if (!success)
            return BadRequest(new { Message = "Booking cannot be approved. Ensure it exists and payment is completed." });

        try
        {
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking != null)
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                if (student?.UserId != null)
                    await _notificationService.SendRealTimeNotificationAsync(student.UserId,
                        "Your booking has been approved by the admin.", NotificationTypes.BookingApproved);

                var landlord = await ResolveLandlordAsync(booking);
                if (landlord?.UserId != null)
                    await _notificationService.SendRealTimeNotificationAsync(landlord.UserId,
                        "A booking on your property has been approved by the admin.", NotificationTypes.BookingApproved);
            }
        }
        catch { }

        return Ok(new { Success = true });
    }

    [HttpPost("reject-booking/{bookingId}")]
    public async Task<IActionResult> RejectBooking(Guid bookingId, [FromBody] AdminContractApprovalRequest? request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized(new { Message = "Admin user not identified" });

        var success = await HttpContext.RequestServices.GetRequiredService<IBookingService>()
            .RejectBookingAsync(bookingId, adminUserId, request?.AdminNotes);

        if (!success)
            return BadRequest(new { Message = "Booking cannot be rejected. Ensure it exists and payment is completed." });

        try
        {
            var booking = await _unitOfWork.Bookings.GetAsync(bookingId);
            if (booking != null)
            {
                var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                if (student?.UserId != null)
                    await _notificationService.SendRealTimeNotificationAsync(student.UserId,
                        "Your booking has been rejected by the admin.", NotificationTypes.BookingRejected);

                var landlord = await ResolveLandlordAsync(booking);
                if (landlord?.UserId != null)
                    await _notificationService.SendRealTimeNotificationAsync(landlord.UserId,
                        "A booking on your property has been rejected by the admin.", NotificationTypes.BookingRejected);
            }
        }
        catch { }

        return Ok(new { Success = true });
    }

    [HttpGet("pending-contracts")]
    public async Task<IActionResult> GetPendingContracts()
    {
        var result = await _adminApprovalService.GetPendingContractsAsync();
        return Ok(result);
    }

    [HttpGet("pending-escrow-releases")]
    public async Task<IActionResult> GetPendingEscrowReleases()
    {
        var result = await _adminApprovalService.GetPendingEscrowReleasesAsync();
        return Ok(result);
    }

    [HttpPost("approve-contract")]
    public async Task<IActionResult> ApproveContract([FromBody] AdminContractApprovalRequest request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized(new { Message = "Admin user not identified" });

        request.AdminUserId = adminUserId;
        var result = await _adminApprovalService.ApproveContractAsync(request);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(result);
    }

    [HttpPost("reject-contract")]
    public async Task<IActionResult> RejectContract([FromBody] AdminContractApprovalRequest request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized(new { Message = "Admin user not identified" });

        request.AdminUserId = adminUserId;
        var result = await _adminApprovalService.RejectContractAsync(request);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(result);
    }

    [HttpPost("release-escrow")]
    public async Task<IActionResult> ReleaseEscrow([FromBody] EscrowReleaseRequest request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized(new { Message = "Admin user not identified" });

        request.AdminUserId = adminUserId;
        var result = await _adminApprovalService.ProcessEscrowReleaseAsync(request);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        try
        {
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow != null)
            {
                var booking = await _unitOfWork.Bookings.GetAsync(escrow.BookingId);
                if (booking != null)
                {
                    var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                    if (student?.UserId != null)
                        await _notificationService.SendRealTimeNotificationAsync(student.UserId,
                            "The escrow has been released for your booking.", NotificationTypes.EscrowReleased);

                    var landlord = await ResolveLandlordAsync(booking);
                    if (landlord?.UserId != null)
                        await _notificationService.SendRealTimeNotificationAsync(landlord.UserId,
                            "The escrow has been released. Payment has been transferred to your account.",
                            NotificationTypes.EscrowReleased);
                }
            }
        }
        catch { }

        return Ok(result);
    }

    [HttpPost("refund-escrow")]
    public async Task<IActionResult> RefundEscrow([FromBody] EscrowRefundRequest request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
            return Unauthorized(new { Message = "Admin user not identified" });

        request.AdminUserId = adminUserId;
        var result = await _adminApprovalService.ProcessEscrowRefundAsync(request);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        try
        {
            var escrow = await _unitOfWork.EscrowTransactions.GetAsync(request.EscrowId);
            if (escrow != null)
            {
                var booking = await _unitOfWork.Bookings.GetAsync(escrow.BookingId);
                if (booking != null)
                {
                    var student = await _unitOfWork.Students.GetAsync(booking.StudentId);
                    if (student?.UserId != null)
                        await _notificationService.SendRealTimeNotificationAsync(student.UserId,
                            "Your escrow has been refunded.", NotificationTypes.EscrowRefunded);

                    var landlord = await ResolveLandlordAsync(booking);
                    if (landlord?.UserId != null)
                        await _notificationService.SendRealTimeNotificationAsync(landlord.UserId,
                            "The escrow has been refunded for a booking on your property.",
                            NotificationTypes.EscrowRefunded);
                }
            }
        }
        catch { }

        return Ok(result);
    }

    private async Task<Domain.Entities.LandLord?> ResolveLandlordAsync(Domain.Entities.Booking booking)
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
            var unit = await _unitOfWork.HousingUnits.GetAsync(booking.HousingUnitId.Value);
            if (unit?.LandLordId is Guid lid3)
                return await _unitOfWork.LandLords.GetAsync(lid3);
        }
        return null;
    }
}
