using Domain.Enums;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for student registration
/// Extends RegisterRequest with student-specific information
/// </summary>
public class StudentRegisterRequest : RegisterRequest
{
    public string? UserName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; } // Use enum value (Male, Female)
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PreferredArea { get; set; }
    public string? NationalId { get; set; }
    public string? ProfileImage { get; set; } // Base64 encoded image or URL
}

/// <summary>
/// Request model for updating student profile
/// </summary>
public class StudentUpdateRequest
{
    public Guid StudentId { get; set; }
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PreferredArea { get; set; }
    public string? NationalId { get; set; }
}

public class StudentDeleteRequest
{
    public Guid StudentId { get; set; }
    public bool IsDeleted { get; set; }
}


/// <summary>
/// Request model for filtering/searching students
/// </summary>
public class StudentFilterRequest
{
    public string? City { get; set; }
    public string? PreferredArea { get; set; }
    public Gender? Gender { get; set; }
    public DateTime? DateOfBirthFrom { get; set; }
    public DateTime? DateOfBirthTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Request model for student password change
/// </summary>
public class ChangePasswordRequest
{
    public Guid StudentId { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
}
