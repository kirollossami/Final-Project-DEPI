using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

/// <summary>
/// Response model for bed details
/// </summary>
public class BedResponse
{
    public Guid BedId { get; set; }
    public Guid RoomId { get; set; }
    public string BedNumber { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsOccupied { get; set; }
    public decimal? CalculatedPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Indexed response for bed listings
/// </summary>
public class BedIndexedResponse : GenericIndexedResponse<BedResponse>
{
}
