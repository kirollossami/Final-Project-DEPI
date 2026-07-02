using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;


// Request model for creating a new room
public class RoomCreateRequest
{
    public Guid HousingUnitId { get; set; }
    public RoomType RoomType { get; set; }
    public string? RoomImageUrl { get; set; }
    public int NumberOfBeds { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public bool IsAvailable { get; set; }
}


// Request model for updating an existing room
public class RoomUpdateRequest
{
    public Guid RoomId { get; set; }
    public RoomType? RoomType { get; set; }
    public string? RoomImageUrl { get; set; }
    public int? NumberOfBeds { get; set; }
    public decimal? Price { get; set; }
    public int? Capacity { get; set; }
    public bool? IsAvailable { get; set; }
}

// Request model for filtering/searching rooms
public class RoomFilterRequest
{
    public Guid? HousingUnitId { get; set; }
    public RoomType? RoomType { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsAvailable { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
