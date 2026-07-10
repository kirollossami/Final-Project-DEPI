using Domain.Enums;

namespace Business.DTOs.Responses;

public class ContractResponse
{
    public Guid ContractId { get; set; }
    public Guid BookingId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string? OriginalContractPdfPath { get; set; }
    public string? StudentSignedContractPath { get; set; }
    public string? LandlordSignedContractPath { get; set; }
    public bool IsStudentSigned { get; set; }
    public bool IsLandlordSigned { get; set; }
    public bool IsAdminApproved { get; set; }
    public DateTime? StudentSignedAt { get; set; }
    public DateTime? LandlordSignedAt { get; set; }
    public DateTime? AdminApprovedAt { get; set; }
    public string? AdminNotes { get; set; }
    public ContractStatus ContractStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
