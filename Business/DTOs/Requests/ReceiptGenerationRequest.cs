using Domain.Enums;

namespace Business.DTOs.Requests;

public class ReceiptGenerationRequest
{
    public Guid PaymentId { get; set; }
    public Guid? EscrowId { get; set; }  // Nullable — null for PaymentReceived receipts (no escrow yet)
    public ReceiptType Type { get; set; }
    public string IssuedToUserId { get; set; } = string.Empty;
    public string IssuedToRole { get; set; } = string.Empty;
    public string IssuedToName { get; set; } = string.Empty;
    public Dictionary<string, object>? AdditionalData { get; set; }
}
