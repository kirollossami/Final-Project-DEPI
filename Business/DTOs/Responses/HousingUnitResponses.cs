using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class HousingUnitResponse
{
    public Guid HousingUnitId { get; set; }
    public Guid LandLordId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Area { get; set; }
    public decimal Price { get; set; }
    public decimal BaseMonthlyPrice { get; set; }
    public string? UnitImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public Gender GenderAllowed { get; set; }
    public string Rules { get; set; }
    public bool IsDeleted { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string Location { get; set; }
    public int NumberOfRooms { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class HousingUnitIndexedResponse : GenericIndexedResponse<HousingUnitResponse>
{
}

public class HousingUnitDetailsResponse
{
    public Guid HousingUnitId { get; set; }
    public Guid LandLordId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Area { get; set; }
    public decimal Price { get; set; }
    public decimal BaseMonthlyPrice { get; set; }
    public string? UnitImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public Gender GenderAllowed { get; set; }
    public string Rules { get; set; }
    public bool IsDeleted { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string Location { get; set; }
    public int NumberOfRooms { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RoomResponse> Rooms { get; set; } = new();
    public List<UnitImageResponse> UnitImages { get; set; } = new();
    public List<ReviewResponse> Reviews { get; set; } = new();
}

public class UnitImageResponse
{
    public Guid UnitImageId { get; set; }
    public Guid HousingUnitId { get; set; }
    public string ImageUrl { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime UploadedAt { get; set; }
}
