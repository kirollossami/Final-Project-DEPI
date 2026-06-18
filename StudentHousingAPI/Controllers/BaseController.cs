using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StudentHousingAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    protected string GetLoggedId()
    {
        var claim = User.FindFirst("UserId")
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("sub");
        return claim?.Value ?? throw new UnauthorizedAccessException("User ID not found in token");
    }

    /// <summary>
    /// Gets the logged-in user's name from JWT claims
    /// </summary>
    protected string GetLoggedUserName()
    {
        var claim = User.FindFirst(ClaimTypes.Name);
        return claim?.Value ?? throw new UnauthorizedAccessException("User name not found in token");
    }

    /// <summary>
    /// Checks if user has a specific role
    /// </summary>
    protected bool HasRole(string role)
    {
        return User.IsInRole(role);
    }
}
