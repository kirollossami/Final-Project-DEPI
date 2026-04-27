using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class RoomResponse
{
    public Guid RoomId { get; set; }
    public Guid HousingUnitId { get; set; }
    public RoomType RoomType { get; set; }
    public string? RoomImageUrl { get; set; }
    public int NumberOfBeds { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

public class RoomIndexedResponse : GenericIndexedResponse<RoomResponse>
{
}
