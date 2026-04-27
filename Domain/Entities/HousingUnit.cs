using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class HousingUnit
    {
        public Guid HousingUnitId { get; set; }
        public Guid LandLordId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public decimal Price { get; set; }
        public string? UnitImageUrl { get; set; }
        public Gender GenderAllowed { get; set; }
        public string Rules { get; set; }
        public bool IsDeleted { get; set; } = false;
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; } = 0;
        public string Location { get; set; }
        public int NumberOfRooms { get; set; }
        public bool IsAvailable { get; set; }
        public virtual LandLord? LandLord { get; set; }

        public virtual ICollection<Room>? Rooms { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }
        public virtual ICollection<Wishlist>? WishlistedBy { get; set; }
    }

}
