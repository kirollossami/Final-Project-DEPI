using Domain.Enums;

namespace Business.DTOs.Requests;

public class ContractGenerationRequest
{
    public Guid BookingId { get; set; }
    public DateTime ReceivingDate { get; set; }
    public DateTime HandoverDate { get; set; }
    public decimal FinalPrice { get; set; }
    public ContractDurationType DurationType { get; set; }
    public int DurationValue { get; set; }
    public string OwnerFullName { get; set; } = string.Empty;
    public string OwnerNationalId { get; set; } = string.Empty;
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentNationalId { get; set; } = string.Empty;
}
