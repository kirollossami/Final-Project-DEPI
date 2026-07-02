namespace Business.DTOs.Requests;

public class PaymobPaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
