using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class CommissionRecord
    {
        public Guid CommissionRecordId { get; set; }
        public Guid BookingId { get; set; }
        public decimal Rate { get; set; }       // e.g. 0.10 for 10%
        public decimal Amount { get; set; }     // TotalPrice × Rate
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Booking? Booking { get; set; }
    }
}
