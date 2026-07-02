namespace Business.DTOs.Responses;

public class ContractResponse
{
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string GeneratedPdfUrl { get; set; } = string.Empty;
    public bool IsStudentSigned { get; set; }
    public bool IsOwnerSigned { get; set; }
    public bool IsAdminApproved { get; set; }
    public DateTime CreatedAt { get; set; }
}
