using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class PricingService : IPricingService
{
    private readonly IHousingUnitRepository _housingUnitRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IBedRepository _bedRepository;

    public PricingService(
        IHousingUnitRepository housingUnitRepository,
        IRoomRepository roomRepository,
        IBedRepository bedRepository)
    {
        _housingUnitRepository = housingUnitRepository;
        _roomRepository = roomRepository;
        _bedRepository = bedRepository;
    }

    public async Task<decimal> CalculateBedPriceAsync(Guid bedId)
    {
        var bed = await _bedRepository.GetAll()
            .Include(b => b.Room)
            .ThenInclude(r => r.HousingUnit)
            .FirstOrDefaultAsync(b => b.BedId == bedId);

        if (bed?.Room?.HousingUnit == null)
            throw new Exception("Bed not found or not associated with a room/unit");

        var roomPrice = await CalculateRoomPriceAsync(bed.Room.RoomId);
        var bedCount = await _bedRepository.GetAll()
            .CountAsync(b => b.RoomId == bed.RoomId && !b.IsDeleted);

        if (bedCount == 0)
            throw new Exception("No beds found in room");

        return roomPrice / bedCount;
    }

    public async Task<decimal> CalculateRoomPriceAsync(Guid roomId)
    {
        var room = await _roomRepository.GetAll()
            .Include(r => r.HousingUnit)
            .FirstOrDefaultAsync(r => r.RoomId == roomId);

        if (room?.HousingUnit == null)
            throw new Exception("Room not found or not associated with a unit");

        var unitPrice = room.HousingUnit.BaseMonthlyPrice > 0
            ? room.HousingUnit.BaseMonthlyPrice
            : room.HousingUnit.Price;

        if (unitPrice <= 0)
            throw new Exception("Housing unit has no valid price set");

        var roomCount = await _roomRepository.GetAll()
            .CountAsync(r => r.HousingUnitId == room.HousingUnitId && !r.IsDeleted);

        if (roomCount == 0)
            throw new Exception("No rooms found in unit");

        return unitPrice / roomCount;
    }

    public async Task<decimal> CalculateUnitPriceAsync(Guid housingUnitId)
    {
        var unit = await _housingUnitRepository.GetAsync(housingUnitId);
        if (unit == null)
            throw new Exception("Unit not found");

        return unit.BaseMonthlyPrice > 0 ? unit.BaseMonthlyPrice : unit.Price;
    }

    public async Task<decimal> CalculateBookingPriceAsync(BookingType bookingType, Guid targetId, DateTime startDate, DateTime endDate)
    {
        var months = CalculateMonths(startDate, endDate);
        if (months <= 0)
            throw new Exception("Invalid booking dates");

        var monthlyPrice = bookingType switch
        {
            BookingType.Bed => await CalculateBedPriceAsync(targetId),
            BookingType.Room => await CalculateRoomPriceAsync(targetId),
            BookingType.Unit => await CalculateUnitPriceAsync(targetId),
            _ => throw new Exception("Invalid booking type")
        };

        return monthlyPrice * months;
    }

    public async Task<Dictionary<Guid, decimal>> CalculateAllBedPricesInRoomAsync(Guid roomId)
    {
        var beds = await _bedRepository.GetAll()
            .Include(b => b.Room)
            .Where(b => b.RoomId == roomId && !b.IsDeleted)
            .ToListAsync();

        var result = new Dictionary<Guid, decimal>();
        foreach (var bed in beds)
        {
            result[bed.BedId] = await CalculateBedPriceAsync(bed.BedId);
        }

        return result;
    }

    public async Task<Dictionary<Guid, decimal>> CalculateAllRoomPricesInUnitAsync(Guid housingUnitId)
    {
        var rooms = await _roomRepository.GetAll()
            .Include(r => r.HousingUnit)
            .Where(r => r.HousingUnitId == housingUnitId && !r.IsDeleted)
            .ToListAsync();

        var result = new Dictionary<Guid, decimal>();
        foreach (var room in rooms)
        {
            result[room.RoomId] = await CalculateRoomPriceAsync(room.RoomId);
        }

        return result;
    }

    private int CalculateMonths(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            return 0;

        var totalDays = (endDate - startDate).Days;
        return (int)Math.Ceiling(totalDays / 30.0); // Approximate months
    }
}
