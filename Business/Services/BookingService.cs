using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetAll()
            .Include(b => b.Student)
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null) return null;

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            StudentId = booking.StudentId,
            RoomId = booking.RoomId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            BookingStatus = booking.BookingStatus
        };
    }

    public async Task<BookingIndexedResponse> GetBookingsAsync(BookingFilterRequest filter)
    {
        var query = _bookingRepository.GetAll().AsQueryable();

        if (filter.StudentId.HasValue)
        {
            query = query.Where(b => b.StudentId == filter.StudentId.Value);
        }

        if (filter.RoomId.HasValue)
        {
            query = query.Where(b => b.RoomId == filter.RoomId.Value);
        }

        if (filter.BookingStatus.HasValue)
        {
            query = query.Where(b => b.BookingStatus == filter.BookingStatus.Value);
        }

        var totalCount = await query.CountAsync();
        var bookings = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new BookingIndexedResponse
        {
            Records = bookings.Select(b => new BookingResponse
            {
                BookingId = b.BookingId,
                StudentId = b.StudentId,
                RoomId = b.RoomId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TotalPrice = b.TotalPrice,
                BookingStatus = b.BookingStatus
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<BookingResponse?> CreateBookingAsync(BookingCreateRequest request)
    {
        var booking = new Domain.Entities.Booking
        {
            BookingId = Guid.NewGuid(),
            StudentId = request.StudentId,
            RoomId = request.RoomId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalPrice = 0, // Will be calculated based on room price and duration
            BookingStatus = BookingStatus.Pending,
            IsDeleted = false
        };

        await _bookingRepository.Insert(booking);
        await _bookingRepository.CommitAsync();

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            StudentId = booking.StudentId,
            RoomId = booking.RoomId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            BookingStatus = booking.BookingStatus
        };
    }

    public async Task<BookingResponse?> UpdateBookingAsync(BookingUpdateRequest request)
    {
        var booking = await _bookingRepository.GetAsync(request.BookingId);
        if (booking == null) return null;

        if (request.StartDate.HasValue)
        {
            booking.StartDate = request.StartDate.Value;
        }

        if (request.EndDate.HasValue)
        {
            booking.EndDate = request.EndDate.Value;
        }

        if (request.BookingStatus.HasValue)
        {
            booking.BookingStatus = request.BookingStatus.Value;
        }

        _bookingRepository.Update(booking);
        await _bookingRepository.CommitAsync();

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            StudentId = booking.StudentId,
            RoomId = booking.RoomId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            BookingStatus = booking.BookingStatus
        };
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetAsync(bookingId);
        if (booking == null) return false;

        booking.BookingStatus = BookingStatus.Cancelled;
        booking.IsDeleted = true;

        _bookingRepository.Update(booking);
        await _bookingRepository.CommitAsync();

        return true;
    }
}
