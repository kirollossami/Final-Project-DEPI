namespace Business.DTOs.Responses;

public class BookingApprovalResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid BookingId { get; set; }
    public Guid? EscrowId { get; set; }
    public decimal AmountTransferred { get; set; }
    public string Currency { get; set; } = "EGP";
    public DateTime ProcessedAt { get; set; }
}
