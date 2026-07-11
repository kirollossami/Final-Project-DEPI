using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class BookingConflictService : IBookingConflictService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IBedRepository _bedRepository;
    private readonly IHousingUnitRepository _housingUnitRepository;

    public BookingConflictService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IBedRepository bedRepository,
        IHousingUnitRepository housingUnitRepository)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _bedRepository = bedRepository;
        _housingUnitRepository = housingUnitRepository;
    }

    // Valid booking statuses that indicate an active booking (not cancelled or rejected)
    private static readonly BookingStatus[] ActiveBookingStatuses = 
    {
        BookingStatus.PendingPayment,
        BookingStatus.WaitingForContract,
        BookingStatus.WaitingForSignatures,
        BookingStatus.WaitingForStudentSignature,
        BookingStatus.WaitingForLandlordSignature,
        BookingStatus.WaitingForAdminApproval,
        BookingStatus.Approved
    };

    public async Task<bool> HasBookingConflictAsync(BookingType bookingType, Guid targetId, DateTime startDate, DateTime endDate)
    {
        var conflicts = await GetConflictsAsync(bookingType, targetId, startDate, endDate);
        return conflicts.Any();
    }

    public async Task<List<BookingConflict>> GetConflictsAsync(BookingType bookingType, Guid targetId, DateTime startDate, DateTime endDate)
    {
        var conflicts = new List<BookingConflict>();

        switch (bookingType)
        {
            case BookingType.Bed:
                conflicts = await GetBedConflictsAsync(targetId, startDate, endDate);
                break;
            case BookingType.Room:
                conflicts = await GetRoomConflictsAsync(targetId, startDate, endDate);
                break;
            case BookingType.Unit:
                conflicts = await GetUnitConflictsAsync(targetId, startDate, endDate);
                break;
            default:
                throw new ArgumentException("Invalid booking type");
        }

        return conflicts;
    }

    public async Task<bool> IsAvailableForBookingAsync(BookingType bookingType, Guid targetId, DateTime startDate, DateTime endDate)
    {
        return !await HasBookingConflictAsync(bookingType, targetId, startDate, endDate);
    }

    private async Task<List<BookingConflict>> GetBedConflictsAsync(Guid bedId, DateTime startDate, DateTime endDate)
    {
        var conflicts = new List<BookingConflict>();

        // Check if the bed itself is booked
        var bedBookings = await _bookingRepository.GetAll()
            .Where(b => b.BedId == bedId &&
                        !b.IsDeleted &&
                        ActiveBookingStatuses.Contains(b.BookingStatus) &&
                        ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                         (b.StartDate <= endDate && b.EndDate >= endDate) ||
                         (b.StartDate >= startDate && b.EndDate <= endDate)))
            .ToListAsync();

        foreach (var booking in bedBookings)
        {
            conflicts.Add(new BookingConflict
            {
                BookingId = booking.BookingId,
                BookingType = booking.BookingType,
                BedId = booking.BedId,
                RoomId = booking.RoomId,
                HousingUnitId = booking.HousingUnitId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                ConflictReason = "Bed is already booked for this period"
            });
        }

        // Check if the room is booked (which would make all beds unavailable)
        var bed = await _bedRepository.GetAll()
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.BedId == bedId && !b.IsDeleted);

        if (bed?.Room != null)
        {
            var roomBookings = await _bookingRepository.GetAll()
                .Where(b => b.RoomId == bed.Room.RoomId &&
                            b.BookingType == BookingType.Room &&
                            !b.IsDeleted &&
                            ActiveBookingStatuses.Contains(b.BookingStatus) &&
                            ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                             (b.StartDate <= endDate && b.EndDate >= endDate) ||
                             (b.StartDate >= startDate && b.EndDate <= endDate)))
                .ToListAsync();

            foreach (var booking in roomBookings)
            {
                conflicts.Add(new BookingConflict
                {
                    BookingId = booking.BookingId,
                    BookingType = booking.BookingType,
                    BedId = booking.BedId,
                    RoomId = booking.RoomId,
                    HousingUnitId = booking.HousingUnitId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    ConflictReason = "Room is booked, making all beds unavailable"
                });
            }
        }

        // Check if the unit is booked (which would make all rooms and beds unavailable)
        if (bed?.Room?.HousingUnitId != null)
        {
            var unitBookings = await _bookingRepository.GetAll()
                .Where(b => b.HousingUnitId == bed.Room.HousingUnitId &&
                            b.BookingType == BookingType.Unit &&
                            !b.IsDeleted &&
                            ActiveBookingStatuses.Contains(b.BookingStatus) &&
                            ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                             (b.StartDate <= endDate && b.EndDate >= endDate) ||
                             (b.StartDate >= startDate && b.EndDate <= endDate)))
                .ToListAsync();

            foreach (var booking in unitBookings)
            {
                conflicts.Add(new BookingConflict
                {
                    BookingId = booking.BookingId,
                    BookingType = booking.BookingType,
                    BedId = booking.BedId,
                    RoomId = booking.RoomId,
                    HousingUnitId = booking.HousingUnitId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    ConflictReason = "Unit is booked, making all rooms and beds unavailable"
                });
            }
        }

        return conflicts;
    }

    private async Task<List<BookingConflict>> GetRoomConflictsAsync(Guid roomId, DateTime startDate, DateTime endDate)
    {
        var conflicts = new List<BookingConflict>();

        // Check if any beds in the room are booked
        var bedIds = await _bedRepository.GetAll()
            .Where(b => b.RoomId == roomId && !b.IsDeleted)
            .Select(b => b.BedId)
            .ToListAsync();

        if (bedIds.Any())
        {
            var bedBookings = await _bookingRepository.GetAll()
                .Where(b => bedIds.Contains(b.BedId.Value) &&
                            b.BookingType == BookingType.Bed &&
                            !b.IsDeleted &&
                            ActiveBookingStatuses.Contains(b.BookingStatus) &&
                            ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                             (b.StartDate <= endDate && b.EndDate >= endDate) ||
                             (b.StartDate >= startDate && b.EndDate <= endDate)))
                .ToListAsync();

            foreach (var booking in bedBookings)
            {
                conflicts.Add(new BookingConflict
                {
                    BookingId = booking.BookingId,
                    BookingType = booking.BookingType,
                    BedId = booking.BedId,
                    RoomId = booking.RoomId,
                    HousingUnitId = booking.HousingUnitId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    ConflictReason = "Bed in room is already booked"
                });
            }
        }

        // Check if the room itself is booked
        var roomBookings = await _bookingRepository.GetAll()
            .Where(b => b.RoomId == roomId &&
                        b.BookingType == BookingType.Room &&
                        !b.IsDeleted &&
                        ActiveBookingStatuses.Contains(b.BookingStatus) &&
                        ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                         (b.StartDate <= endDate && b.EndDate >= endDate) ||
                         (b.StartDate >= startDate && b.EndDate <= endDate)))
            .ToListAsync();

        foreach (var booking in roomBookings)
        {
            conflicts.Add(new BookingConflict
            {
                BookingId = booking.BookingId,
                BookingType = booking.BookingType,
                BedId = booking.BedId,
                RoomId = booking.RoomId,
                HousingUnitId = booking.HousingUnitId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                ConflictReason = "Room is already booked for this period"
            });
        }

        // Check if the unit is booked
        var room = await _roomRepository.GetAsync(roomId);
        if (room?.HousingUnitId != null)
        {
            var unitBookings = await _bookingRepository.GetAll()
                .Where(b => b.HousingUnitId == room.HousingUnitId &&
                            b.BookingType == BookingType.Unit &&
                            !b.IsDeleted &&
                            ActiveBookingStatuses.Contains(b.BookingStatus) &&
                            ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                             (b.StartDate <= endDate && b.EndDate >= endDate) ||
                             (b.StartDate >= startDate && b.EndDate <= endDate)))
                .ToListAsync();

            foreach (var booking in unitBookings)
            {
                conflicts.Add(new BookingConflict
                {
                    BookingId = booking.BookingId,
                    BookingType = booking.BookingType,
                    BedId = booking.BedId,
                    RoomId = booking.RoomId,
                    HousingUnitId = booking.HousingUnitId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    ConflictReason = "Unit is booked, making all rooms and beds unavailable"
                });
            }
        }

        return conflicts;
    }

    private async Task<List<BookingConflict>> GetUnitConflictsAsync(Guid housingUnitId, DateTime startDate, DateTime endDate)
    {
        var conflicts = new List<BookingConflict>();

        // Get all rooms in the unit
        var roomIds = await _roomRepository.GetAll()
            .Where(r => r.HousingUnitId == housingUnitId && !r.IsDeleted)
            .Select(r => r.RoomId)
            .ToListAsync();

        // Get all beds in all rooms
        var bedIds = await _bedRepository.GetAll()
            .Where(b => roomIds.Contains(b.RoomId) && !b.IsDeleted)
            .Select(b => b.BedId)
            .ToListAsync();

        // Check if any beds are booked
        if (bedIds.Any())
        {
            var bedBookings = await _bookingRepository.GetAll()
                .Where(b => bedIds.Contains(b.BedId.Value) &&
                            b.BookingType == BookingType.Bed &&
                            !b.IsDeleted &&
                            ActiveBookingStatuses.Contains(b.BookingStatus) &&
                            ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                             (b.StartDate <= endDate && b.EndDate >= endDate) ||
                             (b.StartDate >= startDate && b.EndDate <= endDate)))
                .ToListAsync();

            foreach (var booking in bedBookings)
            {
                conflicts.Add(new BookingConflict
                {
                    BookingId = booking.BookingId,
                    BookingType = booking.BookingType,
                    BedId = booking.BedId,
                    RoomId = booking.RoomId,
                    HousingUnitId = booking.HousingUnitId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    ConflictReason = "Bed in unit is already booked"
                });
            }
        }

        // Check if any rooms are booked
        if (roomIds.Any())
        {
            var roomBookings = await _bookingRepository.GetAll()
                .Where(b => roomIds.Contains(b.RoomId.Value) &&
                            b.BookingType == BookingType.Room &&
                            !b.IsDeleted &&
                            ActiveBookingStatuses.Contains(b.BookingStatus) &&
                            ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                             (b.StartDate <= endDate && b.EndDate >= endDate) ||
                             (b.StartDate >= startDate && b.EndDate <= endDate)))
                .ToListAsync();

            foreach (var booking in roomBookings)
            {
                conflicts.Add(new BookingConflict
                {
                    BookingId = booking.BookingId,
                    BookingType = booking.BookingType,
                    BedId = booking.BedId,
                    RoomId = booking.RoomId,
                    HousingUnitId = booking.HousingUnitId,
                    StartDate = booking.StartDate,
                    EndDate = booking.EndDate,
                    ConflictReason = "Room in unit is already booked"
                });
            }
        }

        // Check if the unit itself is booked
        var unitBookings = await _bookingRepository.GetAll()
            .Where(b => b.HousingUnitId == housingUnitId &&
                        b.BookingType == BookingType.Unit &&
                        !b.IsDeleted &&
                        ActiveBookingStatuses.Contains(b.BookingStatus) &&
                        ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                         (b.StartDate <= endDate && b.EndDate >= endDate) ||
                         (b.StartDate >= startDate && b.EndDate <= endDate)))
            .ToListAsync();

        foreach (var booking in unitBookings)
        {
            conflicts.Add(new BookingConflict
            {
                BookingId = booking.BookingId,
                BookingType = booking.BookingType,
                BedId = booking.BedId,
                RoomId = booking.RoomId,
                HousingUnitId = booking.HousingUnitId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                ConflictReason = "Unit is already booked for this period"
            });
        }

        return conflicts;
    }
}
