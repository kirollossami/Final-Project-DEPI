using Domain.Enums;

namespace Business.DTOs.Requests;

public class AdminUserFilterRequest
{
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ReviewVerificationRequest
{
    public UniversityVerificationStatus NewStatus { get; set; }
}
