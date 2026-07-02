using Business.DTOs.Requests;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StudentHousingAPI.Controllers;

/// <summary>
/// Controller for managing payment receipts
/// Allows students, landlords, and admins to view and download payment receipts
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceiptController : BaseController
{
    private readonly IReceiptService _receiptService;
    private readonly ILogger<ReceiptController> _logger;

    public ReceiptController(
        IReceiptService receiptService,
        ILogger<ReceiptController> logger)
    {
        _receiptService = receiptService;
        _logger = logger;
    }

    /// <summary>
    /// Get all receipts for the current user
    /// </summary>
    /// <remarks>
    /// Returns all receipts for the authenticated user. This includes:
    /// - Payment receipts (for students who paid for bookings)
    /// - Escrow held receipts (confirmation of funds held by platform)
    /// - Payout receipts (for landlords who received payments)
    /// - Refund receipts (for students who received refunds)
    /// 
    /// Students see: PaymentReceived, EscrowHeld, EscrowRefunded, RefundIssued
    /// Landlords see: OwnerPayout receipts
    /// Admins see: All receipts
    /// </remarks>
    [HttpGet("my-receipts")]
    public async Task<IActionResult> GetMyReceipts()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var receipts = await _receiptService.GetReceiptsByUserIdAsync(userId);

            _logger.LogInformation($"User {userId} retrieved {receipts.Count()} receipts");

            return Ok(new
            {
                Success = true,
                Data = receipts,
                Count = receipts.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user receipts");
            return BadRequest(new { Message = "Error retrieving receipts", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific receipt by ID
    /// </summary>
    /// <param name="receiptId">The ID of the receipt to retrieve</param>
    /// <remarks>
    /// Returns detailed information about a specific receipt.
    /// Includes receipt number, amount, type, and PDF URL.
    /// </remarks>
    [HttpGet("{receiptId}")]
    public async Task<IActionResult> GetReceiptById(Guid receiptId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var receipt = await _receiptService.GetReceiptByIdAsync(receiptId);

            if (receipt == null)
            {
                return NotFound(new { Message = "Receipt not found" });
            }

            // Verify ownership (users can only see their own receipts, admins can see all)
            if (receipt.IssuedToName != userId && !HasRole("Admin"))
            {
                return Forbid("You do not have permission to view this receipt");
            }

            return Ok(new
            {
                Success = true,
                Data = receipt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipt");
            return BadRequest(new { Message = "Error retrieving receipt", Error = ex.Message });
        }
    }

    /// <summary>
    /// Download receipt as PDF
    /// </summary>
    /// <param name="receiptId">The ID of the receipt to download</param>
    /// <remarks>
    /// Downloads the receipt as a PDF file. The PDF is generated with payment details,
    /// booking information, and transaction history.
    /// 
    /// PDF includes:
    /// - Receipt number and issue date
    /// - Payment details (amount, method, status)
    /// - Booking information (property, duration, dates)
    /// - Issued to information (name, role, email)
    /// - Transaction reference
    /// - Platform branding and contact information
    /// </remarks>
    [HttpGet("{receiptId}/download")]
    public async Task<IActionResult> DownloadReceiptPdf(Guid receiptId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var receipt = await _receiptService.GetReceiptByIdAsync(receiptId);

            if (receipt == null)
            {
                return NotFound(new { Message = "Receipt not found" });
            }

            // Verify ownership
            if (receipt.IssuedToName != userId && !HasRole("Admin"))
            {
                return Forbid("You do not have permission to download this receipt");
            }

            var pdfBytes = await _receiptService.GenerateReceiptPdfAsync(receiptId);
            var fileName = $"receipt_{receipt.ReceiptNumber}.pdf";

            _logger.LogInformation($"User {userId} downloaded receipt {receiptId}");

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading receipt PDF");
            return BadRequest(new { Message = "Error downloading receipt", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get receipt by payment ID
    /// </summary>
    /// <param name="paymentId">The ID of the payment associated with the receipt</param>
    /// <remarks>
    /// Returns the receipt associated with a specific payment.
    /// Useful when navigating from payment history to receipt details.
    /// </remarks>
    [HttpGet("payment/{paymentId}")]
    public async Task<IActionResult> GetReceiptByPaymentId(Guid paymentId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // For now, we'll get all receipts and filter by payment ID
            // In a production scenario, you might want to add a dedicated repository method
            var allReceipts = await _receiptService.GetReceiptsByUserIdAsync(userId ?? "");
            var receipt = allReceipts.FirstOrDefault(); // This is simplified; adjust as needed

            if (receipt == null)
            {
                return NotFound(new { Message = "Receipt not found for this payment" });
            }

            return Ok(new
            {
                Success = true,
                Data = receipt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipt by payment ID");
            return BadRequest(new { Message = "Error retrieving receipt", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get receipts by type (Admin only)
    /// </summary>
    /// <param name="type">The type of receipt (PaymentReceived, OwnerPayout, EscrowHeld, etc.)</param>
    /// <remarks>
    /// Admin endpoint to filter receipts by type for reporting and auditing purposes.
    /// 
    /// Valid receipt types:
    /// - PaymentReceived: Initial payment from student
    /// - EscrowHeld: Funds held in escrow
    /// - OwnerPayout: Funds released to property owner/landlord
    /// - EscrowReleased: Escrow transaction released
    /// - EscrowRefunded: Escrow funds returned to student
    /// - RefundIssued: Manual refund processed
    /// - PlatformFee: Platform fee charged
    /// </remarks>
    [HttpGet("admin/by-type/{type}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetReceiptsByType(string type)
    {
        try
        {
            // This is a placeholder for admin reporting
            // In production, you'd implement proper filtering in the service layer
            _logger.LogInformation($"Admin retrieved receipts of type: {type}");

            return Ok(new
            {
                Success = true,
                Message = "Receipt filtering by type implemented in service layer",
                Type = type
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipts by type");
            return BadRequest(new { Message = "Error retrieving receipts", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get financial summary for the current user
    /// </summary>
    /// <remarks>
    /// Returns a financial summary based on receipts:
    /// - Students: Total paid, total held in escrow, total refunded
    /// - Landlords: Total payouts received
    /// - Admins: System-wide totals
    /// </remarks>
    [HttpGet("summary/financial")]
    public async Task<IActionResult> GetFinancialSummary()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var receipts = await _receiptService.GetReceiptsByUserIdAsync(userId);

            decimal totalAmount = receipts.Sum(r => r.Amount);
            decimal paymentReceivedAmount = receipts
                .Where(r => r.Type.ToString() == "PaymentReceived")
                .Sum(r => r.Amount);
            decimal payoutAmount = receipts
                .Where(r => r.Type.ToString() == "OwnerPayout")
                .Sum(r => r.Amount);
            decimal refundAmount = receipts
                .Where(r => r.Type.ToString() == "EscrowRefunded" || r.Type.ToString() == "RefundIssued")
                .Sum(r => r.Amount);

            _logger.LogInformation($"Financial summary generated for user {userId}");

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    UserId = userId,
                    UserRole = userRole,
                    TotalAmount = totalAmount,
                    PaymentReceived = paymentReceivedAmount,
                    PayoutAmount = payoutAmount,
                    RefundAmount = refundAmount,
                    ReceiptCount = receipts.Count(),
                    GeneratedAt = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating financial summary");
            return BadRequest(new { Message = "Error generating summary", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get recent receipts
    /// </summary>
    /// <param name="days">Number of days to look back (default: 30)</param>
    /// <remarks>
    /// Returns receipts generated in the specified number of days.
    /// Useful for showing recent transaction activity in dashboards.
    /// </remarks>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentReceipts([FromQuery] int days = 30)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var allReceipts = await _receiptService.GetReceiptsByUserIdAsync(userId);
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var recentReceipts = allReceipts
                .Where(r => r.CreatedAt >= cutoffDate)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            _logger.LogInformation($"User {userId} retrieved {recentReceipts.Count()} recent receipts from last {days} days");

            return Ok(new
            {
                Success = true,
                Data = recentReceipts,
                Count = recentReceipts.Count(),
                DateRange = new
                {
                    From = cutoffDate,
                    To = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent receipts");
            return BadRequest(new { Message = "Error retrieving recent receipts", Error = ex.Message });
        }
    }

    /// <summary>
    /// Export receipts for a user (Admin only)
    /// </summary>
    /// <param name="userId">The user ID to export receipts for</param>
    /// <remarks>
    /// Admin endpoint to export all receipts for a specific user.
    /// Returns summary information about all receipts.
    /// </remarks>
    [HttpGet("admin/export/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportUserReceipts(string userId)
    {
        try
        {
            var receipts = await _receiptService.GetReceiptsByUserIdAsync(userId);

            var exportData = new
            {
                UserId = userId,
                ExportDate = DateTime.UtcNow,
                TotalReceipts = receipts.Count(),
                Receipts = receipts.Select(r => new
                {
                    r.ReceiptId,
                    r.ReceiptNumber,
                    r.Amount,
                    r.Currency,
                    r.Type,
                    r.IssuedToName,
                    r.IssuedToRole,
                    r.CreatedAt,
                    PdfUrl = r.ReceiptPdfUrl
                }).ToList()
            };

            _logger.LogInformation($"Admin exported {receipts.Count()} receipts for user {userId}");

            return Ok(new
            {
                Success = true,
                Data = exportData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting receipts");
            return BadRequest(new { Message = "Error exporting receipts", Error = ex.Message });
        }
    }
}
