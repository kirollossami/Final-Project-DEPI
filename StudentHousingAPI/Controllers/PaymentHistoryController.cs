using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StudentHousingAPI.Controllers;

/// <summary>
/// Controller for accessing payment history and audit logs
/// Allows users to view their payment transaction history
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentHistoryController : BaseController
{
    private readonly IPaymentHistoryService _paymentHistoryService;
    private readonly ILogger<PaymentHistoryController> _logger;

    public PaymentHistoryController(
        IPaymentHistoryService paymentHistoryService,
        ILogger<PaymentHistoryController> logger)
    {
        _paymentHistoryService = paymentHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get payment history for the current user
    /// </summary>
    [HttpGet("my-history")]
    public async Task<IActionResult> GetMyPaymentHistory()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var history = await _paymentHistoryService.GetUserPaymentHistoryAsync(userId);
            return Ok(new
            {
                Success = true,
                Data = history,
                Count = history.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user payment history");
            return BadRequest(new { Message = "Error retrieving payment history" });
        }
    }

    /// <summary>
    /// Get payment history for a specific booking (Admin only)
    /// </summary>
    [HttpGet("booking/{bookingId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBookingPaymentHistory(Guid bookingId)
    {
        try
        {
            var history = await _paymentHistoryService.GetBookingPaymentHistoryAsync(bookingId);
            return Ok(new
            {
                Success = true,
                Data = history,
                Count = history.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking payment history");
            return BadRequest(new { Message = "Error retrieving booking payment history" });
        }
    }

    /// <summary>
    /// Get payment history for a specific payment transaction
    /// </summary>
    [HttpGet("payment/{paymentId}")]
    public async Task<IActionResult> GetPaymentTransactionHistory(Guid paymentId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            var history = await _paymentHistoryService.GetPaymentTransactionHistoryAsync(paymentId);

            if (!isAdmin)
            {
                history = history.Where(h => h.UserId == userId);
            }

            return Ok(new
            {
                Success = true,
                Data = history,
                Count = history.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment transaction history");
            return BadRequest(new { Message = "Error retrieving payment transaction history" });
        }
    }

    /// <summary>
    /// Get payment history within a date range (Admin only)
    /// </summary>
    [HttpGet("range")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPaymentHistoryByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
            {
                return BadRequest(new { Message = "Start date must be before end date" });
            }

            var history = await _paymentHistoryService.GetPaymentHistoryByDateRangeAsync(startDate, endDate);
            return Ok(new
            {
                Success = true,
                Data = history,
                Count = history.Count(),
                DateRange = new { StartDate = startDate, EndDate = endDate }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment history by date range");
            return BadRequest(new { Message = "Error retrieving payment history" });
        }
    }
}
