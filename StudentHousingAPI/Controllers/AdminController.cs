using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly IAdminService _adminService;
    private readonly IStudentService _studentService;
    private readonly IComplaintService _complaintService;
    private readonly IFileStorageService _fileStorageService;

    public AdminController(
        IAdminService adminService,
        IStudentService studentService,
        IComplaintService complaintService,
        IFileStorageService fileStorageService)
    {
        _adminService = adminService;
        _studentService = studentService;
        _complaintService = complaintService;
        _fileStorageService = fileStorageService;
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
        var result = await _adminService.ToggleUserActiveStatusAsync(userId);
        if (!result.Success)
            return BadRequest(result);
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
}
