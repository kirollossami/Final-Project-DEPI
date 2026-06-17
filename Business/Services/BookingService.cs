using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Business.Settings;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Business.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ICommissionRecordRepository _commissionRecordRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly CommissionSettings _commissionSettings;

    public BookingService(
        IBookingRepository bookingRepository,
        ICommissionRecordRepository commissionRecordRepository,
        IStudentRepository studentRepository,
        IOptions<CommissionSettings> commissionSettings)
    {
        _bookingRepository = bookingRepository;
        _commissionRecordRepository = commissionRecordRepository;
        _studentRepository = studentRepository;
        _commissionSettings = commissionSettings.Value;
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetAll()
            .Include(b => b.Student)
            .Include(b => b.Room)
            .Include(b => b.CommissionRecord)
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
            BookingStatus = booking.BookingStatus,
            CommissionAmount = booking.CommissionRecord?.Amount
        };
    }

    public async Task<BookingIndexedResponse> GetBookingsAsync(BookingFilterRequest filter)
    {
        var query = _bookingRepository.GetAll()
            .Include(b => b.CommissionRecord)
            .AsQueryable();

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
                BookingStatus = b.BookingStatus,
                CommissionAmount = b.CommissionRecord?.Amount
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<BookingIndexedResponse> GetMyBookingsAsync(string userId, BookingStatus? statusFilter, int pageNumber = 1, int pageSize = 10)
    {
        var student = await _studentRepository.GetAll()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
        {
            return new BookingIndexedResponse
            {
                Records = new List<BookingResponse>(),
                TotalRecords = 0,
                PageIndex = pageNumber - 1,
                PageSize = pageSize
            };
        }

        var query = _bookingRepository.GetAll()
            .Include(b => b.CommissionRecord)
            .Where(b => b.StudentId == student.StudentId);

        if (statusFilter.HasValue)
        {
            query = query.Where(b => b.BookingStatus == statusFilter.Value);
        }

        var totalCount = await query.CountAsync();
        var bookings = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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
                BookingStatus = b.BookingStatus,
                IsDeleted = b.IsDeleted,
                CommissionAmount = b.CommissionRecord?.Amount
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        };
    }

    public async Task<BookingResponse?> CreateBookingAsync(BookingCreateRequest request)
    {
        // Calculate total price based on room price and duration
        var room = await _bookingRepository.GetAll()
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.RoomId == request.RoomId);
        
        if (room == null) return null;

        var durationDays = (request.EndDate - request.StartDate).Days;
        if (durationDays <= 0) return null;

        var totalPrice = room.Room?.Price * durationDays ?? 0;

        var booking = new Domain.Entities.Booking
        {
            BookingId = Guid.NewGuid(),
            StudentId = request.StudentId,
            RoomId = request.RoomId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalPrice = totalPrice,
            BookingStatus = BookingStatus.Pending,
            IsDeleted = false
        };

        await _bookingRepository.Insert(booking);
        await _bookingRepository.CommitAsync();

        var commissionRate = _commissionSettings.GlobalRate;
        var commissionAmount = totalPrice * commissionRate;

        var commissionRecord = new CommissionRecord
        {
            CommissionRecordId = Guid.NewGuid(),
            BookingId = booking.BookingId,
            Rate = commissionRate,
            Amount = commissionAmount
        };

        await _commissionRecordRepository.Insert(commissionRecord);
        await _commissionRecordRepository.CommitAsync();

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            StudentId = booking.StudentId,
            RoomId = booking.RoomId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            BookingStatus = booking.BookingStatus,
            CommissionAmount = commissionAmount
        };
    }

    public async Task<BookingResponse?> UpdateBookingAsync(BookingUpdateRequest request)
    {
        var booking = await _bookingRepository.GetAll()
            .Include(b => b.CommissionRecord)
            .FirstOrDefaultAsync(b => b.BookingId == request.BookingId);
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

        await _bookingRepository.Update(booking);
        await _bookingRepository.CommitAsync();

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            StudentId = booking.StudentId,
            RoomId = booking.RoomId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            BookingStatus = booking.BookingStatus,
            CommissionAmount = booking.CommissionRecord?.Amount
        };
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetAsync(bookingId);
        if (booking == null) return false;

        booking.BookingStatus = BookingStatus.Cancelled;
        booking.IsDeleted = true;

        await _bookingRepository.Update(booking);
        await _bookingRepository.CommitAsync();

        return true;
    }
}
