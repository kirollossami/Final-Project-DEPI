using Domain.Enums;

namespace Business.Interfaces;

public interface IPricingService
{
    Task<decimal> CalculateBedPriceAsync(Guid bedId);
    Task<decimal> CalculateRoomPriceAsync(Guid roomId);
    Task<decimal> CalculateUnitPriceAsync(Guid housingUnitId);
    Task<decimal> CalculateBookingPriceAsync(BookingType bookingType, Guid targetId, DateTime startDate, DateTime endDate);
    Task<Dictionary<Guid, decimal>> CalculateAllBedPricesInRoomAsync(Guid roomId);
    Task<Dictionary<Guid, decimal>> CalculateAllRoomPricesInUnitAsync(Guid housingUnitId);
}
