namespace Business.DTOs.Responses;

public class PaymobPaymentResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? OrderId { get; set; }
    public string? IntentionId { get; set; }
    public string? PaymentToken { get; set; }
    public string? PaymentUrl { get; set; }
    public string? RawResponse { get; set; }
}
