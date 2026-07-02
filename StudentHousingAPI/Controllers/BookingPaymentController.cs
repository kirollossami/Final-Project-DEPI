using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingPaymentController : BaseController
{
    private readonly IBookingPaymentService _bookingPaymentService;

    public BookingPaymentController(IBookingPaymentService bookingPaymentService)
    {
        _bookingPaymentService = bookingPaymentService;
    }

    [HttpPost("initiate")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> InitiatePayment([FromBody] BookingPaymentRequest request)
    {
        var result = await _bookingPaymentService.InitiateBookingPaymentAsync(request);
        if (!result.Success)
            return BadRequest(new { Message = result.Message });
        
        return Ok(result);
    }

    [HttpPost("callback")]
    [AllowAnonymous] // Paymob calls this endpoint
    public async Task<IActionResult> PaymentCallback([FromBody] PaymobCallbackRequest request)
    {
        var result = await _bookingPaymentService.ProcessPaymentCallbackAsync(
            request.OrderId,
            request.TransactionId,
            request.IsSuccess);
        
        return Ok(result);
    }

    [HttpPost("complete/{paymentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteWorkflow(Guid paymentId)
    {
        var result = await _bookingPaymentService.CompleteBookingWorkflowAsync(paymentId);
        if (!result.Success)
            return BadRequest(new { Message = result.Message });
        
        return Ok(result);
    }
}

public class PaymobCallbackRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
}
