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
        public string PropertyOwnerShipProof { get; set; }
        public string VerificationStatus { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<HousingUnit>? HousingUnits { get; set; }
    }
}
