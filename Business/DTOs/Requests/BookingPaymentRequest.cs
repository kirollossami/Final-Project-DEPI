using Domain.Enums;

namespace Business.DTOs.Requests;

public class BookingPaymentRequest
{
    public Guid BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Description { get; set; }
}
