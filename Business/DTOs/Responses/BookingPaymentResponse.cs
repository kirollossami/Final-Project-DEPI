namespace Business.DTOs.Responses;

public class BookingPaymentResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ContractId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? ContractPdfUrl { get; set; }
}
