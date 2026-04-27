using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Student
    {
        public Guid StudentId { get; set; }
        public string? UserId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PreferredArea { get; set; }
        public string NationalId { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }
        public virtual ICollection<Complaint>? Complaints { get; set; }
        public virtual ICollection<Wishlist>? Wishlists { get; set; }
    }
}
