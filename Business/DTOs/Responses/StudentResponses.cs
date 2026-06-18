using Domain.Enums;

namespace Business.DTOs.Responses;

public class StudentResponse
{
    public Guid StudentId { get; set; }
    public string? UserId { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PreferredArea { get; set; }
    public string? NationalId { get; set; }
    public string? FacultyName { get; set; }
    public string? UniversityName { get; set; }
    public string? UniversityEmail { get; set; }
    public UniversityVerificationStatus UniversityVerificationStatus { get; set; }
}

public class StudentIndexedResponse : GenericIndexedResponse<StudentResponse>
{
}
