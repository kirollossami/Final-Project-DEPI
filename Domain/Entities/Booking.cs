using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Booking
    {
        public Guid BookingId { get; set; }
        public Guid StudentId { get; set; }
        public Guid RoomId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus BookingStatus { get; set; }
        public bool IsDeleted { get; set; } = false;

        public virtual Student? Student { get; set; }
        public virtual Room? Room { get; set; }
        public virtual Payment? Payment { get; set; }
    }
}
