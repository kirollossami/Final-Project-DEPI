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
        public BookingType BookingType { get; set; }
        public Guid? BedId { get; set; }
        public Guid? RoomId { get; set; }
        public Guid? HousingUnitId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public BookingStatus BookingStatus { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ContractId { get; set; }
        public string? ContractPdfUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual Student? Student { get; set; }
        public virtual Bed? Bed { get; set; }
        public virtual Room? Room { get; set; }
        public virtual HousingUnit? HousingUnit { get; set; }
        public virtual Payment? Payment { get; set; }
        public virtual CommissionRecord? CommissionRecord { get; set; }
    }
}
