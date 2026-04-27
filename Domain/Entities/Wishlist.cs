using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Wishlist
    {
        public Guid WishlistId { get; set; }
        public Guid StudentId { get; set; }
        public Guid HousingUnitId { get; set; }
        public DateTime AddedDate { get; set; }
        public virtual Student Student { get; set; } = null!;
        public virtual HousingUnit HousingUnit { get; set; } = null!;

    }
}
