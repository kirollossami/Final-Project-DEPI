using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace StudentHousingAPI.Controllers;

/// <summary>
/// Controller for managing student complaints
/// Only allows students to file complaints for properties they have active bookings with
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComplaintController : BaseController
{
    private readonly IComplaintService _complaintService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ComplaintController> _logger;

    public ComplaintController(
        IComplaintService complaintService,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<ComplaintController> logger)
    {
        _complaintService = complaintService;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all complaints (Admin only)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllComplaints(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? studentId = null,
        [FromQuery] Guid? housingUnitId = null,
        [FromQuery] ComplaintStatus? status = null)
    {
        try
        {
            var filter = new ComplaintFilterRequest
            {
                StudentId = studentId,
                HousingUnitId = housingUnitId,
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var complaints = await _complaintService.GetComplaintsAsync(filter);

            _logger.LogInformation($"Admin retrieved {complaints.Records.Count()} complaints");

            return Ok(new
            {
                Success = true,
                Data = complaints.Records,
                TotalRecords = complaints.TotalRecords,
                PageIndex = complaints.PageIndex,
                PageSize = complaints.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving complaints");
            return BadRequest(new { Message = "Error retrieving complaints", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get all complaints for the current student
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyComplaints(
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

            // Get student ID from user ID
            var studentId = await GetStudentIdFromUserId(userId);
            if (studentId == null)
            {
                return BadRequest(new { Message = "Student profile not found" });
            }

            var filter = new ComplaintFilterRequest
            {
                StudentId = studentId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var complaints = await _complaintService.GetComplaintsAsync(filter);

            _logger.LogInformation($"Student {studentId} retrieved {complaints.Records.Count()} complaints");

            return Ok(new
            {
                Success = true,
                Data = complaints.Records,
                TotalRecords = complaints.TotalRecords,
                PageIndex = complaints.PageIndex,
                PageSize = complaints.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student complaints");
            return BadRequest(new { Message = "Error retrieving complaints", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific complaint by ID
    /// </summary>
    [HttpGet("{complaintId}")]
    public async Task<IActionResult> GetComplaintById(Guid complaintId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var complaint = await _complaintService.GetComplaintByIdAsync(complaintId);

            if (complaint == null)
            {
                return NotFound(new { Message = "Complaint not found" });
            }

            // Verify ownership (students can only see their own complaints, admins can see all)
            var studentId = await GetStudentIdFromUserId(userId ?? "");
            if (complaint.StudentId != studentId && !HasRole("Admin"))
            {
                return Forbid("You do not have permission to view this complaint");
            }

            return Ok(new
            {
                Success = true,
                Data = complaint
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving complaint");
            return BadRequest(new { Message = "Error retrieving complaint", Error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new complaint
    /// Only allows students to complain about properties they have active bookings with
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateComplaint([FromBody] ComplaintCreateRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            // Get student ID from user ID
            var studentId = await GetStudentIdFromUserId(userId);
            if (studentId == null)
            {
                return BadRequest(new { Message = "Student profile not found" });
            }

            // Validate that student has an active booking with this housing unit
            var hasActiveBooking = await HasActiveBookingWithHousingUnit(studentId.Value, request.HousingUnitId);
            if (!hasActiveBooking)
            {
                return BadRequest(new { 
                    Message = "You can only file complaints for properties you have active bookings with" 
                });
            }

            // Set the student ID to the authenticated student
            request.StudentId = studentId.Value;

            var complaint = await _complaintService.CreateComplaintAsync(request);

            if (complaint == null)
            {
                return BadRequest(new { Message = "Failed to create complaint" });
            }

            // Send notification to landlord about the complaint
            await SendComplaintNotificationToLandlord(complaint.ComplaintId, request.HousingUnitId);

            _logger.LogInformation($"Complaint {complaint.ComplaintId} created by student {studentId}");

            return Ok(new
            {
                Success = true,
                Data = complaint,
                Message = "Complaint created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating complaint");
            return BadRequest(new { Message = "Error creating complaint", Error = ex.Message });
        }
    }

    /// <summary>
    /// Update a complaint (Admin only)
    /// </summary>
    [HttpPut("{complaintId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateComplaint(Guid complaintId, [FromBody] ComplaintUpdateRequest request)
    {
        try
        {
            request.ComplaintId = complaintId;
            var complaint = await _complaintService.UpdateComplaintAsync(request);

            if (complaint == null)
            {
                return NotFound(new { Message = "Complaint not found" });
            }

            // Send notification to student about complaint status update
            await SendComplaintStatusNotification(complaint.ComplaintId, complaint.StudentId, complaint.Status);

            _logger.LogInformation($"Complaint {complaintId} updated by admin");

            return Ok(new
            {
                Success = true,
                Data = complaint,
                Message = "Complaint updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating complaint");
            return BadRequest(new { Message = "Error updating complaint", Error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a complaint (Admin only)
    /// </summary>
    [HttpDelete("{complaintId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteComplaint(Guid complaintId)
    {
        try
        {
            var existing = await _complaintService.GetComplaintByIdAsync(complaintId);
            var result = await _complaintService.DeleteComplaintAsync(complaintId);

            if (!result)
            {
                return NotFound(new { Message = "Complaint not found" });
            }

            try
            {
                if (existing?.StudentId != null)
                {
                    var student = await _unitOfWork.Students.GetAsync(existing.StudentId);
                    if (student?.UserId != null)
                        await _notificationService.SendRealTimeNotificationAsync(student.UserId, "Your complaint has been removed by admin.", NotificationTypes.ComplaintDeleted);
                }
            }
            catch { }

            _logger.LogInformation($"Complaint {complaintId} deleted by admin");

            return Ok(new
            {
                Success = true,
                Message = "Complaint deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting complaint");
            return BadRequest(new { Message = "Error deleting complaint", Error = ex.Message });
        }
    }

    /// <summary>
    /// Get complaints for a specific housing unit (Landlord only)
    /// </summary>
    [HttpGet("housing-unit/{housingUnitId}")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> GetComplaintsByHousingUnit(Guid housingUnitId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { Message = "User not identified" });
            }

            // Validate that the housing unit belongs to this landlord
            var ownsHousingUnit = await OwnsHousingUnit(userId, housingUnitId);
            if (!ownsHousingUnit)
            {
                return Forbid("You do not have permission to view complaints for this housing unit");
            }

            var filter = new ComplaintFilterRequest
            {
                HousingUnitId = housingUnitId,
                PageNumber = 1,
                PageSize = 100
            };

            var complaints = await _complaintService.GetComplaintsAsync(filter);

            return Ok(new
            {
                Success = true,
                Data = complaints.Records,
                TotalRecords = complaints.TotalRecords
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving housing unit complaints");
            return BadRequest(new { Message = "Error retrieving complaints", Error = ex.Message });
        }
    }

    // Helper methods

    private async Task<Guid?> GetStudentIdFromUserId(string userId)
    {
        var student = await _unitOfWork.Students.GetAll()
            .FirstOrDefaultAsync(s => s.UserId == userId);
        return student?.StudentId;
    }

    private async Task<bool> HasActiveBookingWithHousingUnit(Guid studentId, Guid housingUnitId)
    {
        // Check if student has an active booking with this housing unit
        // Active booking statuses: Approved, Active
        var activeStatuses = new[] { BookingStatus.Approved, BookingStatus.Approved };
        
        var hasBooking = await _unitOfWork.Bookings.GetAll()
            .AnyAsync(b => b.StudentId == studentId && 
                         b.HousingUnitId == housingUnitId && 
                         activeStatuses.Contains(b.BookingStatus));
        
        return hasBooking;
    }

    private async Task<bool> OwnsHousingUnit(string userId, Guid housingUnitId)
    {
        var landlord = await _unitOfWork.LandLords.GetAll()
            .FirstOrDefaultAsync(l => l.UserId == userId);
        
        if (landlord == null) return false;
        
        var housingUnit = await _unitOfWork.HousingUnits.GetAsync(housingUnitId);
        return housingUnit != null && housingUnit.LandLordId == landlord.LandLordId;
    }

    private async Task SendComplaintNotificationToLandlord(Guid complaintId, Guid housingUnitId)
    {
        try
        {
            var housingUnit = await _unitOfWork.HousingUnits.GetAsync(housingUnitId);
            if (housingUnit == null) return;

            var landlord = await _unitOfWork.LandLords.GetAsync(housingUnit.LandLordId);
            if (landlord == null || string.IsNullOrEmpty(landlord.UserId)) return;

            await _notificationService.SendRealTimeNotificationAsync(
                landlord.UserId,
                $"A new complaint has been filed for your property. Complaint ID: {complaintId}",
                NotificationTypes.NewComplaintFiled
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending complaint notification to landlord");
        }
    }

    private async Task SendComplaintStatusNotification(Guid complaintId, Guid studentId, ComplaintStatus status)
    {
        try
        {
            var student = await _unitOfWork.Students.GetAsync(studentId);
            if (student == null || string.IsNullOrEmpty(student.UserId)) return;

            var statusMessage = status switch
            {
                ComplaintStatus.Open => "Your complaint is being reviewed",
                ComplaintStatus.InInvestigation => "Your complaint is being investigated",
                ComplaintStatus.Resolved => "Your complaint has been resolved",
                _ => "Your complaint status has been updated"
            };

            await _notificationService.SendRealTimeNotificationAsync(
                student.UserId,
                $"Complaint {complaintId} status updated: {statusMessage}",
                NotificationTypes.ComplaintStatusUpdated
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending complaint status notification to student");
        }
    }
}
