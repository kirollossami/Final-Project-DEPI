using Domain.Enums;

namespace Business.DTOs.Responses;

public class EscrowResponse
{
    public Guid EscrowId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ContractId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public EscrowStatus Status { get; set; }
    public decimal PlatformFee { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleaseTransactionId { get; set; }
}
