using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BedController : ControllerBase
{
    private readonly IBedService _bedService;

    public BedController(IBedService bedService)
    {
        _bedService = bedService;
    }

    [HttpGet("GetById/{bedId}")]
    public async Task<ActionResult<BedResponse>> GetBedById(Guid bedId)
    {
        var bed = await _bedService.GetBedByIdAsync(bedId);
        if (bed == null)
        {
            return NotFound();
        }
        return Ok(bed);
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult<BedIndexedResponse>> GetBeds([FromQuery] BedFilterRequest filter)
    {
        var beds = await _bedService.GetBedsAsync(filter);
        return Ok(beds);
    }

    [HttpGet("GetByRoom/{roomId}")]
    public async Task<ActionResult<List<BedResponse>>> GetBedsByRoomId(Guid roomId)
    {
        var beds = await _bedService.GetBedsByRoomIdAsync(roomId);
        return Ok(beds);
    }

    [HttpPost("Create")]
    [Authorize(Policy = "RequireManagerRole")]
    public async Task<ActionResult<BedResponse>> CreateBed([FromBody] BedCreateRequest request)
    {
        var bed = await _bedService.CreateBedAsync(request);
        if (bed == null)
        {
            return BadRequest("Room not found");
        }
        return CreatedAtAction(nameof(GetBedById), new { bedId = bed.BedId }, bed);
    }

    [HttpPut("Update")]
    [Authorize(Policy = "RequireManagerRole")]
    public async Task<ActionResult<BedResponse>> UpdateBed([FromBody] BedUpdateRequest request)
    {
        var bed = await _bedService.UpdateBedAsync(request);
        if (bed == null)
        {
            return NotFound();
        }
        return Ok(bed);
    }

    [HttpDelete("Delete/{bedId}")]
    [Authorize(Policy = "RequireManagerRole")]
    public async Task<ActionResult> DeleteBed(Guid bedId)
    {
        var result = await _bedService.DeleteBedAsync(bedId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}
