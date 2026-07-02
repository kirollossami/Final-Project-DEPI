namespace Business.DTOs.Responses;

public class PaymentHistoryResponse
{
    public Guid HistoryId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? EscrowTransactionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
