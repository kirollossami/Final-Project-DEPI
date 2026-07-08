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
public class BookingController : BaseController
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("GetById/{bookingId}")]
    public async Task<ActionResult<BookingResponse>> GetBookingById(Guid bookingId)
    {
        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound();
        }
        return Ok(booking);
    }

    [HttpGet("GetAll")]
    public async Task<ActionResult<BookingIndexedResponse>> GetBookings([FromQuery] BookingFilterRequest filter)
    {
        var bookings = await _bookingService.GetBookingsAsync(filter);
        return Ok(bookings);
    }

    [HttpPost("Create")]
    public async Task<ActionResult<BookingResponse>> CreateBooking([FromBody] BookingCreateRequest request)
    {
        try
        {
            var booking = await _bookingService.CreateBookingAsync(request);
            if (booking == null)
            {
                return BadRequest("Invalid booking request or booking conflict");
            }
            return CreatedAtAction(nameof(GetBookingById), new { bookingId = booking.BookingId }, booking);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("Update")]
    public async Task<ActionResult<BookingResponse>> UpdateBooking([FromBody] BookingUpdateRequest request)
    {
        var booking = await _bookingService.UpdateBookingAsync(request);
        if (booking == null)
        {
            return NotFound();
        }
        return Ok(booking);
    }

    [HttpDelete("Cancel/{bookingId}")]
    public async Task<ActionResult> CancelBooking(Guid bookingId)
    {
        var result = await _bookingService.CancelBookingAsync(bookingId);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("MultiRoom")]
    public async Task<ActionResult<List<BookingResponse?>>> CreateMultiRoomBooking([FromBody] MultiRoomBookingCreateRequest request)
    {
        var result = await _bookingService.CreateMultiRoomBookingAsync(request);
        return Ok(result);
    }
}
