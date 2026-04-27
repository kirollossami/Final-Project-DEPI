using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class BookingResponse
{
    public Guid BookingId { get; set; }
    public Guid StudentId { get; set; }
    public Guid RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public bool IsDeleted { get; set; }
}

public class BookingIndexedResponse : GenericIndexedResponse<BookingResponse>
{
}
