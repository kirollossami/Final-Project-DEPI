using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UnitImage
    {
        public Guid UnitImageId { get; set; }
        public Guid HousingUnitId { get; set; }
        public string ImageUrl { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public virtual HousingUnit? HousingUnit { get; set; }
    }
}
