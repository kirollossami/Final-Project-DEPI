namespace Business.DTOs.Responses;

/// <summary>
/// JWT Token response model
/// </summary>
public class TokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Authentication response model
/// </summary>
public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TokenResponse? Token { get; set; }
    public UserResponse? User { get; set; }
    public bool RequiresTwoFactor { get; set; }
}

/// <summary>
/// 2FA setup response model
/// </summary>
public class TwoFactorSetupResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Secret { get; set; }
    public string? QrCodeUri { get; set; }
}

/// <summary>
/// User information response
/// </summary>
public class UserResponse
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string[]? Roles { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? LandLordId { get; set; }
}

/// <summary>
/// Generic API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string[]? Errors { get; set; }
}
