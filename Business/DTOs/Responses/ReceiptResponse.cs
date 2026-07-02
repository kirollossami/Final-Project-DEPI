using Domain.Enums;

namespace Business.DTOs.Responses;

public class ReceiptResponse
{
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public ReceiptType Type { get; set; }
    public string IssuedToName { get; set; } = string.Empty;
    public string IssuedToRole { get; set; } = string.Empty;
    public string ReceiptPdfUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
