using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminApprovalController : BaseController
{
    private readonly IAdminApprovalService _adminApprovalService;

    public AdminApprovalController(IAdminApprovalService adminApprovalService)
    {
        _adminApprovalService = adminApprovalService;
    }

    // Admin actions for booking approval/rejection (only for bookings with paid payments)
    [HttpPost("approve-booking/{bookingId}")]
    public async Task<IActionResult> ApproveBooking(Guid bookingId, [FromBody] AdminContractApprovalRequest? request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized(new { Message = "Admin user not identified" });
        }

        var success = await HttpContext.RequestServices.GetRequiredService<IBookingService>()
            .ApproveBookingAsync(bookingId, adminUserId, request?.AdminNotes);

        if (!success)
            return BadRequest(new { Message = "Booking cannot be approved. Ensure it exists and payment is completed." });

        return Ok(new { Success = true });
    }

    [HttpPost("reject-booking/{bookingId}")]
    public async Task<IActionResult> RejectBooking(Guid bookingId, [FromBody] AdminContractApprovalRequest? request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized(new { Message = "Admin user not identified" });
        }

        var success = await HttpContext.RequestServices.GetRequiredService<IBookingService>()
            .RejectBookingAsync(bookingId, adminUserId, request?.AdminNotes);

        if (!success)
            return BadRequest(new { Message = "Booking cannot be rejected. Ensure it exists and payment is completed." });

        return Ok(new { Success = true });
    }

    [HttpGet("pending-contracts")]
    public async Task<IActionResult> GetPendingContracts()
    {
        var result = await _adminApprovalService.GetPendingContractsAsync();
        return Ok(result);
    }

    [HttpGet("pending-escrow-releases")]
    public async Task<IActionResult> GetPendingEscrowReleases()
    {
        var result = await _adminApprovalService.GetPendingEscrowReleasesAsync();
        return Ok(result);
    }

    [HttpPost("approve-contract")]
    public async Task<IActionResult> ApproveContract([FromBody] AdminContractApprovalRequest request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized(new { Message = "Admin user not identified" });
        }

        request.AdminUserId = adminUserId;
        var result = await _adminApprovalService.ApproveContractAsync(request);
        
        if (!result.Success)
            return BadRequest(new { Message = result.Message });
        
        return Ok(result);
    }

    [HttpPost("reject-contract")]
    public async Task<IActionResult> RejectContract([FromBody] AdminContractApprovalRequest request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized(new { Message = "Admin user not identified" });
        }

        request.AdminUserId = adminUserId;
        var result = await _adminApprovalService.RejectContractAsync(request);
        
        if (!result.Success)
            return BadRequest(new { Message = result.Message });
        
        return Ok(result);
    }

    [HttpPost("release-escrow")]
    public async Task<IActionResult> ReleaseEscrow([FromBody] EscrowReleaseRequest request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized(new { Message = "Admin user not identified" });
        }

        request.AdminUserId = adminUserId;
        var result = await _adminApprovalService.ProcessEscrowReleaseAsync(request);
        
        if (!result.Success)
            return BadRequest(new { Message = result.Message });
        
        return Ok(result);
    }

    [HttpPost("refund-escrow")]
    public async Task<IActionResult> RefundEscrow([FromBody] EscrowRefundRequest request)
    {
        var adminUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized(new { Message = "Admin user not identified" });
        }

        request.AdminUserId = adminUserId;
        var result = await _adminApprovalService.ProcessEscrowRefundAsync(request);
        
        if (!result.Success)
            return BadRequest(new { Message = result.Message });
        
        return Ok(result);
    }
}
