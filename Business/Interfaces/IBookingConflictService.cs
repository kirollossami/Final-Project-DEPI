using Domain.Enums;

namespace Business.Interfaces;

public interface IBookingConflictService
{
    Task<bool> HasBookingConflictAsync(BookingType bookingType, Guid targetId, DateTime startDate, DateTime endDate);
    Task<List<BookingConflict>> GetConflictsAsync(BookingType bookingType, Guid targetId, DateTime startDate, DateTime endDate);
    Task<bool> IsAvailableForBookingAsync(BookingType bookingType, Guid targetId, DateTime startDate, DateTime endDate);
}

public class BookingConflict
{
    public Guid BookingId { get; set; }
    public BookingType BookingType { get; set; }
    public Guid? BedId { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? HousingUnitId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ConflictReason { get; set; }
}
