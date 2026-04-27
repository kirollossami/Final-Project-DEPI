using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IBookingService
{
    Task<BookingResponse?> GetBookingByIdAsync(Guid bookingId);
    Task<BookingIndexedResponse> GetBookingsAsync(BookingFilterRequest filter);
    Task<BookingResponse?> CreateBookingAsync(BookingCreateRequest request);
    Task<BookingResponse?> UpdateBookingAsync(BookingUpdateRequest request);
    Task<bool> CancelBookingAsync(Guid bookingId);
}
