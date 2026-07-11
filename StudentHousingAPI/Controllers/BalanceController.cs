using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BalanceController : ControllerBase
{
    private readonly IBalanceService _balanceService;

    public BalanceController(IBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    /// <summary>
    /// Get the authenticated user's own balance.
    /// Works for Admin, LandLord, and Student roles.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyBalance()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { Message = "User not identified." });

        var balance = await _balanceService.GetFullBalanceByUserIdAsync(userId);
        if (balance == null)
            return Ok(new
            {
                AvailableBalance = 0m,
                TotalReceived = 0m,
                TotalPaidOut = 0m,
                Currency = "EGP",
                Message = "No balance record found. Balance will be created on first transaction."
            });

        return Ok(new
        {
            balance.AvailableBalance,
            balance.TotalReceived,
            balance.TotalPaidOut,
            balance.Currency,
            balance.UpdatedAt,
            balance.CreatedAt
        });
    }

    /// <summary>
    /// Admin only: get all balances in the system.
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllBalances()
    {
        var balances = await _balanceService.GetAllBalancesAsync();
        return Ok(balances.Select(b => new
        {
            b.BalanceId,
            b.UserId,
            b.UserRole,
            b.AvailableBalance,
            b.TotalReceived,
            b.TotalPaidOut,
            b.Currency,
            b.UpdatedAt,
            b.CreatedAt
        }));
    }

    /// <summary>
    /// Admin only: get balance for a specific user by their Identity User ID.
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBalanceByUserId(string userId)
    {
        var balance = await _balanceService.GetFullBalanceByUserIdAsync(userId);
        if (balance == null)
            return Ok(new
            {
                AvailableBalance = 0m,
                TotalReceived = 0m,
                TotalPaidOut = 0m,
                Currency = "EGP",
                Message = "No balance record found for this user."
            });

        return Ok(new
        {
            balance.BalanceId,
            balance.UserId,
            balance.UserRole,
            balance.AvailableBalance,
            balance.TotalReceived,
            balance.TotalPaidOut,
            balance.Currency,
            balance.UpdatedAt,
            balance.CreatedAt
        });
    }
}
