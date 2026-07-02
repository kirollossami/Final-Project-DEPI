using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Domain.Enums;

namespace Business.Interfaces;

public interface IBookingService
{
    Task<BookingResponse?> GetBookingByIdAsync(Guid bookingId);
    Task<BookingIndexedResponse> GetBookingsAsync(BookingFilterRequest filter);
    Task<BookingIndexedResponse> GetMyBookingsAsync(string userId, BookingStatus? statusFilter, int pageNumber = 1, int pageSize = 10);
    Task<BookingResponse?> CreateBookingAsync(BookingCreateRequest request);
    Task<List<BookingResponse?>> CreateMultiRoomBookingAsync(MultiRoomBookingCreateRequest request);
    Task<BookingResponse?> UpdateBookingAsync(BookingUpdateRequest request);
    Task<bool> CancelBookingAsync(Guid bookingId);
    Task MarkBookingAsPaidAsync(Guid bookingId);
}
