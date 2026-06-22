using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomController : BaseController
{
    private readonly IRoomService _roomService;

    public RoomController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet("GetById/{id}")]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        var result = await _roomService.GetRoomByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetRooms([FromQuery] RoomFilterRequest filter)
    {
        var result = await _roomService.GetRoomsAsync(filter);
        return Ok(result);
    }

    [HttpPost("Create")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> CreateRoom([FromBody] RoomCreateRequest request)
    {
        var result = await _roomService.CreateRoomAsync(request);
        if (result == null)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Your account is pending admin approval." });
        return Ok(result);
    }

    [HttpPut("Update")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> UpdateRoom([FromBody] RoomUpdateRequest request)
    {
        var result = await _roomService.UpdateRoomAsync(request);
        if (result == null)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Your account is pending admin approval." });
        return Ok(result);
    }

    [HttpDelete("Delete/{id}")]
    [Authorize(Roles = "LandLord")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        if (room == null)
            return NotFound(new { Message = "Room Not Found!" });

        var result = await _roomService.DeleteRoomAsync(id);
        if (!result)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Your account is pending admin approval." });

        return Ok(new { Message = "Room deleted successfully." });
    }
}
