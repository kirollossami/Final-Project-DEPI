using Business.DTOs.Responses;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingApprovalController : ControllerBase
{
    private readonly IBookingApprovalService _bookingApprovalService;
    private readonly ILogger<BookingApprovalController> _logger;

    public BookingApprovalController(
        IBookingApprovalService bookingApprovalService,
        ILogger<BookingApprovalController> logger)
    {
        _bookingApprovalService = bookingApprovalService;
        _logger = logger;
    }

    /// <summary>
    /// Admin approves a booking after both parties have signed the contract.
    /// Transfers funds from admin balance to landlord balance.
    /// </summary>
    [HttpPost("approve/{bookingId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingApprovalResponse>> ApproveBooking(
        Guid bookingId,
        [FromBody] BookingApprovalRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminUserId))
                return Unauthorized("Admin user ID not found");

            var result = await _bookingApprovalService.ApproveBookingAsync(
                bookingId,
                adminUserId,
                request.AdminNotes);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving booking {BookingId}", bookingId);
            return StatusCode(500, new { message = "An error occurred while approving the booking" });
        }
    }

    /// <summary>
    /// Admin rejects a booking after both parties have signed the contract.
    /// Refunds funds from admin balance to student balance.
    /// </summary>
    [HttpPost("reject/{bookingId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingApprovalResponse>> RejectBooking(
        Guid bookingId,
        [FromBody] BookingRejectionRequest request)
    {
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminUserId))
                return Unauthorized("Admin user ID not found");

            var result = await _bookingApprovalService.RejectBookingAsync(
                bookingId,
                adminUserId,
                request.RejectionReason);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting booking {BookingId}", bookingId);
            return StatusCode(500, new { message = "An error occurred while rejecting the booking" });
        }
    }
}

public class BookingApprovalRequest
{
    public string? AdminNotes { get; set; }
}

public class BookingRejectionRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}
