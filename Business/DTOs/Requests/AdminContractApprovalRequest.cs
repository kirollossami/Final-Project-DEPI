namespace Business.DTOs.Requests;

public class AdminContractApprovalRequest
{
    public Guid ContractId { get; set; }
    public string AdminUserId { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public bool IsApproved { get; set; }
}
