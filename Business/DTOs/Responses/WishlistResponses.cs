using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class WishlistResponse
{
    public Guid WishlistId { get; set; }
    public Guid StudentId { get; set; }
    public Guid HousingUnitId { get; set; }
    public DateTime AddedDate { get; set; }
}

public class WishlistIndexedResponse : GenericIndexedResponse<WishlistResponse>
{
}
