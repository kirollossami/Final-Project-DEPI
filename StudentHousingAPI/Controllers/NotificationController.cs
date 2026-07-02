using Business.DTOs.Requests;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StudentHousingAPI.Controllers;

/// <summary>
/// Controller for managing user notifications
/// Allows students, landlords, and admins to receive and manage notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : BaseController
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all notifications for the current user
    /// </summary>
    /// <remarks>
    /// Returns paginated notifications for the authenticated user.
    /// Includes:
    /// - Payment initiated/completed notifications
    /// - Contract ready/approved/rejected notifications
    /// - Escrow release/refund notifications
    /// - Account verification notifications
    /// - Admin approval notifications
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null,
        [FromQuery] bool? isSeen = null)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var filter = new NotificationFilterRequest
            {
                UserId = userId,
                Type = type,
                IsSeen = isSeen,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var notifications = await _notificationService.GetNotificationsAsync(filter);

            _logger.LogInformation($"User {userId} retrieved {notifications.Records.Count()} notifications");

            return Ok(new
            {
                Success = true,
                Data = notifications.Records,
                TotalRecords = notifications.TotalRecords,
                PageIndex = notifications.PageIndex,
                PageSize = notifications.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return BadRequest(new { Message = "Error retrieving notifications", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific notification by ID
    /// </summary>
    /// <param name="notificationId">The ID of the notification to retrieve</param>
    [HttpGet("{notificationId}")]
    public async Task<IActionResult> GetNotificationById(Guid notificationId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notification = await _notificationService.GetNotificationByIdAsync(notificationId);

            if (notification == null)
            {
                return NotFound(new { Message = "Notification not found" });
            }

            // Verify ownership
            if (notification.UserId != userId && !HasRole("Admin"))
            {
                return Forbid("You do not have permission to view this notification");
            }

            return Ok(new
            {
                Success = true,
                Data = notification
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification");
            return BadRequest(new { Message = "Error retrieving notification", Error = ex.Message });
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <remarks>
    /// Updates the notification's read status. Used to track which notifications
    /// the user has viewed.
    /// </remarks>
    [HttpPut("{notificationId}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notification = await _notificationService.GetNotificationByIdAsync(notificationId);

            if (notification == null)
            {
                return NotFound(new { Message = "Notification not found" });
            }

            // Verify ownership
            if (notification.UserId != userId && !HasRole("Admin"))
            {
                return Forbid("You do not have permission to update this notification");
            }

            var result = await _notificationService.MarkAsReadAsync(notificationId);

            if (result)
            {
                _logger.LogInformation($"Notification {notificationId} marked as read by user {userId}");
                return Ok(new
                {
                    Success = true,
                    Message = "Notification marked as read"
                });
            }

            return BadRequest(new { Message = "Failed to mark notification as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return BadRequest(new { Message = "Error marking notification as read", Error = ex.Message });
        }
    }

    /// <summary>
    /// Mark all unread notifications as read
    /// </summary>
    [HttpPut("mark-all-as-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var filter = new NotificationFilterRequest
            {
                UserId = userId,
                IsSeen = false,
                PageSize = 1000 // Get all unread
            };

            var unreadNotifications = await _notificationService.GetNotificationsAsync(filter);
            int markedCount = 0;

            foreach (var notification in unreadNotifications.Records)
            {
                if (await _notificationService.MarkAsReadAsync(notification.NotificationId))
                {
                    markedCount++;
                }
            }

            _logger.LogInformation($"User {userId} marked {markedCount} notifications as read");

            return Ok(new
            {
                Success = true,
                Message = $"{markedCount} notifications marked as read"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return BadRequest(new { Message = "Error marking notifications as read", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    /// <remarks>
    /// Returns the number of unread notifications for the current user.
    /// Useful for displaying notification badges in the UI.
    /// </remarks>
    [HttpGet("count/unread")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var filter = new NotificationFilterRequest
            {
                UserId = userId,
                IsSeen = false,
                PageSize = 1 // Just need count
            };

            var unreadNotifications = await _notificationService.GetNotificationsAsync(filter);

            return Ok(new
            {
                Success = true,
                UnreadCount = unreadNotifications.TotalRecords
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return BadRequest(new { Message = "Error getting unread count", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get notifications by type
    /// </summary>
    /// <remarks>
    /// Filters notifications by type. Common types include:
    /// - PaymentInitiated
    /// - PaymentCompleted
    /// - ContractReady
    /// - ContractApprovedByAdmin
    /// - ContractRejected
    /// - EscrowReleased
    /// - PayoutProcessed
    /// - BookingConfirmed
    /// - AccountVerified
    /// - AccountRejected
    /// </remarks>
    [HttpGet("filter/by-type/{type}")]
    public async Task<IActionResult> GetNotificationsByType(
        string type,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            var filter = new NotificationFilterRequest
            {
                UserId = userId,
                Type = type,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var notifications = await _notificationService.GetNotificationsAsync(filter);

            return Ok(new
            {
                Success = true,
                Data = notifications.Records,
                TotalRecords = notifications.TotalRecords,
                PageIndex = notifications.PageIndex,
                PageSize = notifications.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering notifications");
            return BadRequest(new { Message = "Error filtering notifications", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get admin notifications (Admin only)
    /// </summary>
    /// <remarks>
    /// Returns notifications for admin users. This includes:
    /// - Pending account verifications
    /// - Contracts awaiting approval
    /// - Transaction disputes
    /// - System alerts
    /// </remarks>
    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                return Unauthorized(new { Message = "Admin not identified" });
            }

            // Admin notifications for pending approvals
            var filter = new NotificationFilterRequest
            {
                UserId = adminId,
                Type = "PendingApproval",
                IsSeen = false,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var notifications = await _notificationService.GetNotificationsAsync(filter);

            return Ok(new
            {
                Success = true,
                Data = notifications.Records,
                TotalRecords = notifications.TotalRecords,
                PageIndex = notifications.PageIndex,
                PageSize = notifications.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin notifications");
            return BadRequest(new { Message = "Error retrieving admin notifications", Error = ex.Message });
        }
    }
}
