namespace Business.DTOs.Requests;

public class EscrowRefundRequest
{
    public Guid EscrowId { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public string RefundReason { get; set; } = string.Empty;
}
