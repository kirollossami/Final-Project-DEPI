using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Complaint
    {
        public Guid ComplaintId { get; set; }
        public Guid StudentId { get; set; }
        public Guid LandLordId { get; set; }
        public string Description { get; set; }
        public ComplaintStatus Status { get; set; } // Enum: Open, InProgress, Resolved
        public DateTime CreatedDate { get; set; }

        public virtual Student? Student { get; set; }
        public virtual LandLord? LandLord { get; set; }
    }
}
