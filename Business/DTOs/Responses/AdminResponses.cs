using Domain.Enums;

namespace Business.DTOs.Responses;

public class AdminUserResponse
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string[]? Roles { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? LandLordId { get; set; }
}

public class AdminUserIndexedResponse : GenericIndexedResponse<AdminUserResponse> { }

public class CommissionReportResponse
{
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
    public decimal AverageCommission { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<CommissionRecordResponse>? Records { get; set; }
}

public class CommissionRecordResponse
{
    public Guid CommissionRecordId { get; set; }
    public Guid BookingId { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
