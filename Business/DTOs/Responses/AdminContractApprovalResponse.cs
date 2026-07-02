namespace Business.DTOs.Responses;

public class AdminContractApprovalResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? EscrowId { get; set; }
    public bool IsOwnerPayoutProcessed { get; set; }
    public bool IsEscrowRefunded { get; set; }
}
