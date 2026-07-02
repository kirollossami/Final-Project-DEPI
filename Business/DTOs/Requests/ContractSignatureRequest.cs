namespace Business.DTOs.Requests;

public class ContractSignatureRequest
{
    public Guid ContractId { get; set; }
    public string SignedPdfUrl { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Student" or "Owner"
}
