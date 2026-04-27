using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Requests;

/// <summary>
/// Request model for creating a new booking
/// </summary>
public class BookingCreateRequest
{
    public Guid StudentId { get; set; }
    public Guid RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Request model for updating an existing booking
/// </summary>
public class BookingUpdateRequest
{
    public Guid BookingId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public BookingStatus? BookingStatus { get; set; }
}

/// <summary>
/// Request model for cancelling a booking
/// </summary>
public class BookingCancelRequest
{
    public Guid BookingId { get; set; }
}

/// <summary>
/// Request model for filtering/searching bookings
/// </summary>
public class BookingFilterRequest
{
    public Guid? StudentId { get; set; }
    public Guid? RoomId { get; set; }
    public BookingStatus? BookingStatus { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
