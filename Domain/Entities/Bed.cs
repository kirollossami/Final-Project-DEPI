using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Bed
    {
        public Guid BedId { get; set; }
        public Guid RoomId { get; set; }
        public string BedNumber { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsOccupied { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual Room? Room { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; }
    }
}
