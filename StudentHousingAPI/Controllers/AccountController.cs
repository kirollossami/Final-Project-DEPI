using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AccountController : Controller
{
    private readonly IAuthService authService;
    private readonly IConfiguration _configuration;

    public AccountController(IAuthService authService, IConfiguration configuration)
    {
        this.authService = authService;
        _configuration = configuration;
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
    [AllowAnonymous]
    public IActionResult GoogleChallenge([FromQuery] string returnUrl = null)
    {
        var callbackUrl = Url.Action(nameof(GoogleCallback), "Account", null, Request.Scheme);
        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl,
            Items = { { "scheme", "Google" } }
        };

        // Secure returnUrl validation to prevent open-redirect vulnerabilities
        if (!string.IsNullOrEmpty(returnUrl) && IsSafeRedirectUrl(returnUrl))
        {
            properties.Items["returnUrl"] = returnUrl;
        }
        else
        {
            var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<string[]>();
            var defaultFrontend = allowedOrigins?.FirstOrDefault() ?? "http://localhost:4200";
            properties.Items["returnUrl"] = defaultFrontend;
        }

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var acceptHeader = Request.Headers["Accept"].ToString() ?? string.Empty;
        bool isBrowserNavigation = acceptHeader.Contains("text/html");

        var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<string[]>();
        var frontendUrl = allowedOrigins?.FirstOrDefault() ?? "http://localhost:4200";
        frontendUrl = frontendUrl.TrimEnd('/');

        // Determine where to redirect back on success or error
        var returnUrl = authenticateResult.Properties?.Items.TryGetValue("returnUrl", out var rUrl) == true ? rUrl : null;
        var targetRedirectUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : $"{frontendUrl}/login";

        string getRedirectUrlWithError(string errorName)
        {
            var separator = targetRedirectUrl.Contains("?") ? "&" : "?";
            return $"{targetRedirectUrl}{separator}error={Uri.EscapeDataString(errorName)}";
        }

        string getRedirectUrlWithTokens(string token, string rToken)
        {
            var separator = targetRedirectUrl.Contains("?") ? "&" : "?";
            return $"{targetRedirectUrl}{separator}success=true&token={Uri.EscapeDataString(token)}&refreshToken={Uri.EscapeDataString(rToken)}";
        }

        if (!authenticateResult.Succeeded)
        {
            if (isBrowserNavigation)
            {
                return Redirect(getRedirectUrlWithError("google_auth_failed"));
            }
            return BadRequest(new { error = "Google authentication failed. Please login using the google-challenge endpoint in a browser." });
        }

        // External claims will be validated below (email/provider key)

        // Extract claims from the external principal (do not rely on id_token)
        var provider = "Google";
        var providerKey = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? authenticateResult.Principal.FindFirst("sub")?.Value;

        var email = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = authenticateResult.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(providerKey) || string.IsNullOrEmpty(email))
        {
            if (isBrowserNavigation)
            {
                return Redirect(getRedirectUrlWithError("missing_external_claims"));
            }
            return BadRequest(new { error = "Required external claims (provider key or email) are missing." });
        }

        // Call the Google login service using claims (server-side will create or link account and issue JWT)
        var response = await authService.GoogleLoginAsync(new GoogleLoginRequest
        {
            Provider = provider,
            ProviderKey = providerKey,
            Email = email,
            Name = name
        });

        if (!response.Success)
        {
            if (isBrowserNavigation)
            {
                return Redirect(getRedirectUrlWithError(response.Message ?? "google_login_failed"));
            }
            return BadRequest(new { error = response.Message ?? "Google login failed." });
        }

        // Handle 2FA required scenario
        if (response.RequiresTwoFactor)
        {
            if (isBrowserNavigation)
            {
                var verify2faUrl = $"{frontendUrl}/login/verify-2fa?email={Uri.EscapeDataString(email)}&returnUrl={Uri.EscapeDataString(targetRedirectUrl)}";
                return Redirect(verify2faUrl);
            }
            return Ok(response);
        }

        // Redirect to frontend with the tokens as query parameters
        var accessToken = response.Token?.AccessToken;
        var refreshToken = response.Token?.RefreshToken;
        
        if (isBrowserNavigation)
        {
            return Redirect(getRedirectUrlWithTokens(accessToken ?? "", refreshToken ?? ""));
        }
        return Ok(response);
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

    [HttpPost("send-email-confirmation")]
    [Authorize]
    public async Task<IActionResult> SendEmailConfirmation()
    {
        var userId = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await authService.SendEmailConfirmationAsync(userId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] string userId, [FromQuery] string token)
    {
        var result = await authService.ConfirmEmailAsync(userId, token);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await authService.ForgotPasswordAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await authService.ResetPasswordAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
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

    private bool IsSafeRedirectUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;

        if (Url.IsLocalUrl(url)) return true;

        var allowedOrigins = _configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var origin = $"{uri.Scheme}://{uri.Authority}";
            foreach (var allowedOrigin in allowedOrigins)
            {
                if (allowedOrigin.TrimEnd('/').Equals(origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
