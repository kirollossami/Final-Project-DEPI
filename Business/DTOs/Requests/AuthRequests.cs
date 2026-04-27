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
