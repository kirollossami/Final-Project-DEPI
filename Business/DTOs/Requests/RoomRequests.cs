using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for creating a new room
/// </summary>
public class RoomCreateRequest
{
    public Guid HousingUnitId { get; set; }
    public RoomType RoomType { get; set; }
    public string? RoomImageUrl { get; set; }
    public int NumberOfBeds { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>
/// Request model for updating an existing room
/// </summary>
public class RoomUpdateRequest
{
    public Guid RoomId { get; set; }
    public RoomType? RoomType { get; set; }
    public string? RoomImageUrl { get; set; }
    public int? NumberOfBeds { get; set; }
    public decimal? Price { get; set; }
    public bool? IsAvailable { get; set; }
}

/// <summary>
/// Request model for filtering/searching rooms
/// </summary>
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
