using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for adding an item to wishlist
/// </summary>
public class WishlistCreateRequest
{
    public Guid StudentId { get; set; }
    public Guid HousingUnitId { get; set; }
}

/// <summary>
/// Request model for removing an item from wishlist
/// </summary>
public class WishlistDeleteRequest
{
    public Guid WishlistId { get; set; }
}

/// <summary>
/// Request model for filtering/searching wishlists
/// </summary>
public class WishlistFilterRequest
{
    public Guid? StudentId { get; set; }
    public Guid? HousingUnitId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
