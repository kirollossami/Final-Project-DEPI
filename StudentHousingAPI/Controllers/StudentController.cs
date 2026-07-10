using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class StudentController : BaseController
{
    private readonly IStudentService studentService;
    private readonly IBookingService bookingService;
    private readonly INotificationService _notificationService;

    public StudentController(
        IStudentService studentService,
        IBookingService bookingService,
        INotificationService notificationService)
    {
        this.studentService = studentService;
        this.bookingService = bookingService;
        _notificationService = notificationService;
    }

    #region manipulation
    [HttpPost("Create")]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateStudent(StudentRegisterRequest request)
    {
        var student = await studentService.CreateStudentAsync(request);

        return Ok(student);
    }

    [HttpPut("Update")]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStudent(StudentUpdateRequest request)
    {
        var student = await studentService.UpdateStudentAsync(request);
        try { await _notificationService.SendRealTimeNotificationAsync(GetLoggedId(), "Your profile has been updated.", NotificationTypes.ProfileUpdated); } catch { }
        return Ok(student);
    }

    [HttpPut("ChangePassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await studentService.ChangePasswordAsync(request);
        try { await _notificationService.SendRealTimeNotificationAsync(GetLoggedId(), "Your password has been changed.", NotificationTypes.PasswordChanged); } catch { }
        return Ok();
    }

    [HttpDelete("Delete/{studentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteStudent(Guid studentId)
    {
        await studentService.DeleteStudentAsync(studentId);

        return Ok();
    }
    #endregion

    #region retrieval
    [HttpGet("GetById/{studentId}")]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentById(Guid studentId)
    {
        var student = await studentService.GetStudentByIdAsync(studentId);

        return Ok(student);
    }

    [HttpGet("GetByUserId/{userId}")]
    [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentByUserId(string userId)
    {
        var student = await studentService.GetStudentByUserIdAsync(userId);

        return Ok(student);
    }


    [HttpGet("GetStudents")]
    [ProducesResponseType(typeof(StudentIndexedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudents(StudentFilterRequest filter)
    {
        var students = await studentService.GetStudentsAsync(filter);

        return Ok(students);
    }
    #endregion

    #region administrative
    [HttpPost("Deactivate/{studentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateStudent(Guid studentId)
    {
        var student = await studentService.DeactivateStudentAsync(studentId);
        try
        {
            var studentData = await studentService.GetStudentByIdAsync(studentId);
            if (studentData?.UserId != null)
                await _notificationService.SendRealTimeNotificationAsync(studentData.UserId, "Your account has been deactivated by admin.", NotificationTypes.AccountDeactivated);
        }
        catch { }
        return Ok(student);
    }

    [HttpPost("Reactivate/{studentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReactivateStudent(Guid studentId)
    {
        var student = await studentService.ReactivateStudentAsync(studentId);
        try
        {
            var studentData = await studentService.GetStudentByIdAsync(studentId);
            if (studentData?.UserId != null)
                await _notificationService.SendRealTimeNotificationAsync(studentData.UserId, "Your account has been reactivated by admin.", NotificationTypes.AccountReactivated);
        }
        catch { }
        return Ok(student);
    }

    [HttpPost("ValidateNationalId/{nationalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateNationalId(string nationalId)
    {
        var isValid = await studentService.ValidateNationalIdAsync(nationalId);

        return Ok(isValid);
    }
    #endregion

    [HttpPost("SubmitUniversityVerification")]
    public async Task<IActionResult> SubmitUniversityVerification(
        [FromForm] SubmitUniversityVerificationRequest request,
        IFormFile universityIdCard)
    {
        if (universityIdCard == null || universityIdCard.Length == 0)
            return BadRequest(new { Message = "University ID card file is required." });

        var userId = GetLoggedId();

        var result = await studentService.SubmitUniversityVerificationAsync(
            userId, request, universityIdCard.OpenReadStream(), universityIdCard.FileName);

        if (result == null)
            return BadRequest(new { Message = "Invalid file type. Only JPEG and PNG files are allowed, max 5MB." });

        try { await _notificationService.SendNotificationToRoleAsync("Admin", "A student has submitted their university verification.", NotificationTypes.UniversityVerificationSubmitted); } catch { }
        return Ok(result);
    }

    [HttpGet("MyBookings")]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] BookingStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetLoggedId();
        var result = await bookingService.GetMyBookingsAsync(
            userId, status, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpPost("MultiRoomBooking")]
    public async Task<IActionResult> CreateMultiRoomBooking([FromBody] MultiRoomBookingCreateRequest request)
    {
        var result = await bookingService.CreateMultiRoomBookingAsync(request);
        if (result != null && result.Any())
        {
            try { await _notificationService.SendRealTimeNotificationAsync(GetLoggedId(), "Your multi-room booking has been created.", NotificationTypes.MultiRoomBookingCreated); } catch { }
        }
        return Ok(result);
    }
}

