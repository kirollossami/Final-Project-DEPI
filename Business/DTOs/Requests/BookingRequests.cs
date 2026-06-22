using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;


//Request model for creating a new booking
public class BookingCreateRequest
{
    public Guid StudentId { get; set; }
    public BookingType BookingType { get; set; }
    public Guid? BedId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? HousingUnitId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class MultiRoomBookingCreateRequest
{
    public Guid StudentId { get; set; }
    public List<Guid> RoomIds { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}


//Request model for updating an existing booking
public class BookingUpdateRequest
{
    public Guid BookingId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public BookingStatus? BookingStatus { get; set; }
}


// Request model for cancelling a booking
public class BookingCancelRequest
{
    public Guid BookingId { get; set; }
}


// Request model for filtering/searching bookings
public class BookingFilterRequest
{
    public Guid? StudentId { get; set; }
    public BookingType? BookingType { get; set; }
    public Guid? BedId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? HousingUnitId { get; set; }
    public BookingStatus? BookingStatus { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
