using Domain.Enums;

namespace Business.DTOs.Responses;

public class ReceiptResponse
{
    public Guid ReceiptId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid BookingId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public ReceiptType Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public string IssuedToUserId { get; set; } = string.Empty;
    public string IssuedToName { get; set; } = string.Empty;
    public string IssuedToRole { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string ReceiptPdfUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
