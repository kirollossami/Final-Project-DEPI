namespace Business.DTOs.Requests;


/// <summary>
/// Request model for landlord registration
/// Extends RegisterRequest with landlord-specific information
/// </summary>
public class LandLordRegisterRequest : RegisterRequest
{
    public string FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? NationalId { get; set; }
    public string? PropertyOwnerShipProof { get; set; }
    public string? ProfileImage { get; set; } // Base64 encoded image or URL
}

/// <summary>
/// Request model for updating landlord profile
/// </summary>
public class UpdateLandLordRequest
{
    public Guid LandLordId { get; set; }
    public string? CompanyName { get; set; }
}

/// <summary>
/// Request model for uploading property ownership proof
/// </summary>
public class UploadProofRequest
{
    public string? ProofDocumentPath { get; set; }
    public string? ProofDocumentType { get; set; }
}

/// <summary>
/// Request model for filtering/searching landlords
/// </summary>
public class LandLordFilterRequest
{
    public string? CompanyName { get; set; }
    public string? VerificationStatus { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
