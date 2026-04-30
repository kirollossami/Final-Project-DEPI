using System.ComponentModel.DataAnnotations;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}

/// <summary>
/// Request model for Google OAuth login
/// </summary>
public class GoogleLoginRequest
{
    [Required]
    public string? IdToken { get; set; }
}

/// <summary>
<<<<<<< HEAD
/// Request model for 2FA verification
/// </summary>
public class TwoFactorVerifyRequest
{
    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Code { get; set; }
}

/// <summary>
/// Request model for enabling 2FA
/// </summary>
public class EnableTwoFactorRequest
{
    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Code { get; set; }
}

/// <summary>
=======
>>>>>>> d373b1145cc825f184dd583507a557a4aaf9a1f0
/// Request model for refreshing JWT token
/// </summary>
public class RefreshTokenRequest
{
    public string? Token { get; set; } 
    public string? RefreshToken { get; set; } 
}

/// <summary>
/// Base user registration request
/// </summary>
public class RegisterRequest
{
    public string? Email { get; set; } 
    public string? PhoneNumber { get; set; } 
    public string? Password { get; set; } 
    public string? ConfirmPassword { get; set; } 
}

/// <summary>
/// Request model for logging out
/// </summary>
public class LogoutRequest
{
    public string? Token { get; set; } 
}
