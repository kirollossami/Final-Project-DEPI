using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for creating a new housing unit
/// </summary>
public class HousingUnitCreateRequest
{
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
    public string Location { get; set; }
    public int NumberOfRooms { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>
/// Request model for updating an existing housing unit
/// </summary>
public class HousingUnitUpdateRequest
{
    public Guid HousingUnitId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Area { get; set; }
    public decimal? Price { get; set; }
    public string? UnitImageUrl { get; set; }
    public Gender? GenderAllowed { get; set; }
    public string? Rules { get; set; }
    public string? Location { get; set; }
    public int? NumberOfRooms { get; set; }
    public bool? IsAvailable { get; set; }
}

/// <summary>
/// Request model for filtering/searching housing units
/// </summary>
public class HousingUnitFilterRequest
{
    public Guid? LandLordId { get; set; }
    public string? City { get; set; }
    public string? Area { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public Gender? GenderAllowed { get; set; }
    public bool? IsAvailable { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
