using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class LandLord
    {
        public Guid LandLordId { get; set; }
        public string? UserId { get; set; }
        public string? CompanyName { get; set; }
        public string NationalId { get; set; }
        public string NationalIdImageUrl { get; set; } = string.Empty;
        public string PropertyOwnerShipProof { get; set; }
        public string HousingUnitDocumentationUrl { get; set; } = string.Empty;
        public string VerificationStatus { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<HousingUnit>? HousingUnits { get; set; }
    }
}
