using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AccountController : Controller
{
    private readonly IAuthService authService;

    public AccountController(IAuthService authService)
    {
        this.authService = authService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var response = await authService.LoginAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("google-challenge")]
    public IActionResult GoogleChallenge()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback)),
            Items = { { "scheme", "Google" } }
        };

        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync("Google");

        if (!authenticateResult.Succeeded)
        {
            return Redirect("/login?error=google_auth_failed");
        }

        var email = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return Redirect("/login?error=no_email_provided");
        }

        // Get the Google ID token from the authentication result
        var idToken = authenticateResult.Properties.GetTokenValue("id_token");

        if (string.IsNullOrEmpty(idToken))
        {
            return Redirect("/login?error=no_id_token");
        }

        // Call the Google login service with the ID token
        var response = await authService.GoogleLoginAsync(new GoogleLoginRequest { IdToken = idToken });

        if (!response.Success)
        {
            return Redirect($"/login?error={Uri.EscapeDataString(response.Message ?? "google_login_failed")}");
        }

        // Redirect to frontend with the tokens as query parameters
        // In production, you might want to use a different approach like setting cookies or using a secure token exchange
        var accessToken = response.Token?.AccessToken;
        var refreshToken = response.Token?.RefreshToken;
        
        return Redirect($"/login?success=true&token={Uri.EscapeDataString(accessToken ?? "")}&refreshToken={Uri.EscapeDataString(refreshToken ?? "")}");
    }

    [HttpPost("google-login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var response = await authService.GoogleLoginAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("register/student")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterStudent(StudentRegisterRequest request)
    {
        var response = await authService.RegisterStudentAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("register/landlord")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterLandLord(LandLordRegisterRequest request)
    {
        var response = await authService.RegisterLandLordAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var response = await authService.RefreshTokenAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new ApiResponse<string> { Success = false, Message = "Token is required" });
        }

        var success = await authService.LogoutAsync(token);

        if (!success)
            return BadRequest(new ApiResponse<string> { Success = false, Message = "Logout failed" });

        return Ok(new ApiResponse<string> { Success = true, Message = "Logged out successfully" });
    }

    [HttpPost("2fa/setup")]
    [ProducesResponseType(typeof(TwoFactorSetupResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetupTwoFactor([FromBody] string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new ApiResponse<string> { Success = false, Message = "Email is required" });
        }

        var response = await authService.SetupTwoFactorAsync(email);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("2fa/enable")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorRequest request)
    {
        var response = await authService.EnableTwoFactorAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("2fa/verify")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyTwoFactor(TwoFactorVerifyRequest request)
    {
        var response = await authService.VerifyTwoFactorAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}
