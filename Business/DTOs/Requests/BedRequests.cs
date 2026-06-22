using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for creating a new bed
/// </summary>
public class BedCreateRequest
{
    public Guid RoomId { get; set; }
    public string BedNumber { get; set; }
}

/// <summary>
/// Request model for updating an existing bed
/// </summary>
public class BedUpdateRequest
{
    public Guid BedId { get; set; }
    public string? BedNumber { get; set; }
    public bool? IsAvailable { get; set; }
    public bool? IsOccupied { get; set; }
}

/// <summary>
/// Request model for filtering/searching beds
/// </summary>
public class BedFilterRequest
{
    public Guid? RoomId { get; set; }
    public Guid? HousingUnitId { get; set; }
    public bool? IsAvailable { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
