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
public class HousingUnitController : BaseController
{
    private readonly IHousingUnitService _housingUnitService;
    private readonly INotificationService _notificationService;

    public HousingUnitController(IHousingUnitService housingUnitService, INotificationService notificationService)
    {
        _housingUnitService = housingUnitService;
        _notificationService = notificationService;
    }

    [HttpGet("map-pins")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMapPins()
    {
        var result = await _housingUnitService.GetMapPinsAsync();
        return Ok(result);
    }

    [HttpGet("GetById/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHousingUnit(Guid id)
    {
        var result = await _housingUnitService.GetHousingUnitByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("GetDetailsById/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHousingUnitDetails(Guid id)
    {
        var result = await _housingUnitService.GetHousingUnitDetailsAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("GetAll")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHousingUnits([FromQuery] HousingUnitFilterRequest filter)
    {
        var result = await _housingUnitService.GetHousingUnitsAsync(filter);
        return Ok(result);
    }

    [HttpPost("Create")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> CreateHousingUnit([FromBody] HousingUnitCreateRequest request)
    {
        var result = await _housingUnitService.CreateHousingUnitAsync(request);
        if (result == null)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Your account is pending admin approval." });
        try { await _notificationService.SendNotificationToRoleAsync("Admin", $"A new housing unit \"{request.Title}\" has been created and needs review.", NotificationTypes.NewHousingUnit); } catch { }
        return Ok(result);
    }

    [HttpPut("Update")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> UpdateHousingUnit([FromBody] HousingUnitUpdateRequest request)
    {
        var result = await _housingUnitService.UpdateHousingUnitAsync(request);
        if (result == null)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Your account is pending admin approval." });
        try { await _notificationService.SendNotificationToRoleAsync("Admin", $"Housing unit has been updated.", NotificationTypes.HousingUnitUpdated); } catch { }
        return Ok(result);
    }

    [HttpDelete("Delete/{id}")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> DeleteHousingUnit(Guid id)
    {
        var result = await _housingUnitService.DeleteHousingUnitAsync(id);
        if (!result)
            return NotFound();
        try { await _notificationService.SendNotificationToRoleAsync("Admin", $"A housing unit has been deleted.", NotificationTypes.HousingUnitDeleted); } catch { }
        return Ok();
    }
}
