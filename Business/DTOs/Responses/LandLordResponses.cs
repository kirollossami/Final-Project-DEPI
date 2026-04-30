using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class LandLordResponse
{
    public Guid LandLordId { get; set; }
    public string? UserId { get; set; }
    public string? CompanyName { get; set; }
    public string NationalId { get; set; }
    public string PropertyOwnerShipProof { get; set; }
    public string VerificationStatus { get; set; }
}

public class LandLordIndexedResponse : GenericIndexedResponse<LandLordResponse>
{
}
