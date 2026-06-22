using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LandLordController : BaseController
{
    private readonly ILandLordService _landLordService;
    private readonly IBookingService _bookingService;

    public LandLordController(
        ILandLordService landLordService,
        IBookingService bookingService)
    {
        _landLordService = landLordService;
        _bookingService = bookingService;
    }

    [HttpGet("GetById/{id}")]
    public async Task<IActionResult> GetLandLord(Guid id)
    {
        var result = await _landLordService.GetLandLordByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("GetByUserId/{userId}")]
    public async Task<IActionResult> GetLandLordByUserId(string userId)
    {
        var result = await _landLordService.GetLandLordByUserIdAsync(userId);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetLandLords([FromQuery] LandLordFilterRequest filter)
    {
        var result = await _landLordService.GetLandLordsAsync(filter);
        return Ok(result);
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateLandLord([FromBody] LandLordRegisterRequest request)
    {
        var landlord = await _landLordService.CreateLandLordAsync(request);
        if (landlord == null)
            return BadRequest(new { Message = "Failed to create landlord. Email may already be in use." });
        return Ok(landlord);
    }

    [HttpPut("Update")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> UpdateLandLord([FromBody] UpdateLandLordRequest request)
    {
        var result = await _landLordService.UpdateLandLordAsync(request);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPut("ChangePassword")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = GetLoggedId();
        await _landLordService.ChangePasswordAsync(userId, request);
        return Ok();
    }

    [HttpPost("UploadNationalId")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> UploadNationalId(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "National ID file is required." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(new { Message = "Invalid file type. Only JPEG, PNG, and PDF files are allowed." });

        var userId = GetLoggedId();
        var result = await _landLordService.UploadNationalIdAsync(userId, file.OpenReadStream(), file.FileName);

        if (result == null)
            return BadRequest(new { Message = "Failed to upload National ID." });

        return Ok(result);
    }

    [HttpPost("UploadUnitDocumentation")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> UploadUnitDocumentation(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "Unit documentation file is required." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(new { Message = "Invalid file type. Only JPEG, PNG, and PDF files are allowed." });

        var userId = GetLoggedId();
        var result = await _landLordService.SubmitHousingUnitDocumentationAsync(
            userId, new SubmitHousingUnitDocumentationRequest(), file.OpenReadStream(), file.FileName);

        if (result == null)
            return BadRequest(new { Message = "Failed to upload unit documentation." });

        return Ok(result);
    }

    [HttpGet("MyBookings")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] BookingStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetLoggedId();
        var result = await _bookingService.GetMyBookingsAsync(
            userId, status, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpDelete("Delete/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLandLord(Guid id)
    {
        var result = await _landLordService.DeleteLandLordAsync(id);
        if (!result)
            return NotFound();
        return Ok();
    }

    [HttpPost("Deactivate/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateLandLord(Guid id)
    {
        var result = await _landLordService.DeactivateLandLordAsync(id);
        if (!result)
            return NotFound();
        return Ok();
    }

    [HttpPost("Reactivate/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReactivateLandLord(Guid id)
    {
        var result = await _landLordService.ReactivateLandLordAsync(id);
        if (!result)
            return NotFound();
        return Ok();
    }

    [HttpGet("account-status")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> GetAccountStatus()
    {
        var userId = GetLoggedId();
        var status = await _landLordService.GetAccountStatusAsync(userId);
        return Ok(new { Status = status });
    }
}
