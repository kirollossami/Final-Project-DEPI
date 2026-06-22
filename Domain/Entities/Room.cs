using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Room
    {
        public Guid RoomId { get; set; }
        public Guid HousingUnitId { get; set; }
        public RoomType RoomType { get; set; }
        public string? RoomImageUrl { get; set; }
        public int NumberOfBeds { get; set; }
        public decimal Price { get; set; }
        public decimal PricePerMonth { get; set; }
        public int Capacity { get; set; }
        public int CurrentOccupancy { get; set; } = 0;
        public bool IsAvailable { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual HousingUnit? HousingUnit { get; set; }
        public virtual ICollection<Bed>? Beds { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; } = new List<Booking>();

    }
}
