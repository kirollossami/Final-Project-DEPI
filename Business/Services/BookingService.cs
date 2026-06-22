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
    private readonly IPricingService _pricingService;
    private readonly IBookingConflictService _bookingConflictService;
    private readonly IChatService _chatService;
    private readonly CommissionSettings _commissionSettings;

    public BookingService(
        IBookingRepository bookingRepository,
        ICommissionRecordRepository commissionRecordRepository,
        IStudentRepository studentRepository,
        IPricingService pricingService,
        IBookingConflictService bookingConflictService,
        IChatService chatService,
        IOptions<CommissionSettings> commissionSettings)
    {
        _bookingRepository = bookingRepository;
        _commissionRecordRepository = commissionRecordRepository;
        _studentRepository = studentRepository;
        _pricingService = pricingService;
        _bookingConflictService = bookingConflictService;
        _chatService = chatService;
        _commissionSettings = commissionSettings.Value;
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetAll()
            .Include(b => b.Student)
            .Include(b => b.Room)
            .Include(b => b.Bed)
            .Include(b => b.HousingUnit)
            .Include(b => b.CommissionRecord)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null) return null;

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            StudentId = booking.StudentId,
            BookingType = booking.BookingType,
            BedId = booking.BedId,
            RoomId = booking.RoomId,
            HousingUnitId = booking.HousingUnitId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            BookingStatus = booking.BookingStatus,
            IsDeleted = booking.IsDeleted,
            CommissionAmount = booking.CommissionRecord?.Amount,
            ContractId = booking.ContractId,
            ContractPdfUrl = booking.ContractPdfUrl,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
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

        if (filter.BookingType.HasValue)
        {
            query = query.Where(b => b.BookingType == filter.BookingType.Value);
        }

        if (filter.BedId.HasValue)
        {
            query = query.Where(b => b.BedId == filter.BedId.Value);
        }

        if (filter.RoomId.HasValue)
        {
            query = query.Where(b => b.RoomId == filter.RoomId.Value);
        }

        if (filter.HousingUnitId.HasValue)
        {
            query = query.Where(b => b.HousingUnitId == filter.HousingUnitId.Value);
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
                BookingType = b.BookingType,
                BedId = b.BedId,
                RoomId = b.RoomId,
                HousingUnitId = b.HousingUnitId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TotalPrice = b.TotalPrice,
                BookingStatus = b.BookingStatus,
                IsDeleted = b.IsDeleted,
                CommissionAmount = b.CommissionRecord?.Amount,
                ContractId = b.ContractId,
                ContractPdfUrl = b.ContractPdfUrl,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
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
                BookingType = b.BookingType,
                BedId = b.BedId,
                RoomId = b.RoomId,
                HousingUnitId = b.HousingUnitId,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TotalPrice = b.TotalPrice,
                BookingStatus = b.BookingStatus,
                IsDeleted = b.IsDeleted,
                CommissionAmount = b.CommissionRecord?.Amount,
                ContractId = b.ContractId,
                ContractPdfUrl = b.ContractPdfUrl,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        };
    }

    public async Task<BookingResponse?> CreateBookingAsync(BookingCreateRequest request)
    {
        // Validate that exactly one booking target is provided
        var targetCount = new[] { request.BedId, request.RoomId, request.HousingUnitId }.Count(id => id.HasValue);
        if (targetCount != 1)
        {
            return null;
        }

        // Determine the target ID based on booking type
        Guid? targetId = request.BookingType switch
        {
            BookingType.Bed => request.BedId,
            BookingType.Room => request.RoomId,
            BookingType.Unit => request.HousingUnitId,
            _ => null
        };

        if (targetId == null)
        {
            return null;
        }

        // Calculate total price using the pricing service
        decimal totalPrice = 0;
        try
        {
            totalPrice = await _pricingService.CalculateBookingPriceAsync(
                request.BookingType,
                targetId.Value,
                request.StartDate,
                request.EndDate);
        }
        catch
        {
            return null;
        }

        // Check for booking conflicts
        var hasConflict = await _bookingConflictService.HasBookingConflictAsync(
            request.BookingType,
            targetId.Value,
            request.StartDate,
            request.EndDate);

        if (hasConflict)
        {
            return null;
        }

        var booking = new Domain.Entities.Booking
        {
            BookingId = Guid.NewGuid(),
            StudentId = request.StudentId,
            BookingType = request.BookingType,
            BedId = request.BedId,
            RoomId = request.RoomId,
            HousingUnitId = request.HousingUnitId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalPrice = totalPrice,
            BookingStatus = BookingStatus.Pending,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
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

        var student = await _studentRepository.GetAsync(request.StudentId);
        if (student?.UserId != null)
        {
            try
            {
                await _chatService.GetOrCreateConversationAsync(booking.BookingId, student.UserId);
            }
            catch
            {
                // Non-critical: conversation creation failure should not break booking
            }
        }

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            StudentId = booking.StudentId,
            BookingType = booking.BookingType,
            BedId = booking.BedId,
            RoomId = booking.RoomId,
            HousingUnitId = booking.HousingUnitId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            BookingStatus = booking.BookingStatus,
            IsDeleted = booking.IsDeleted,
            CommissionAmount = commissionAmount,
            ContractId = booking.ContractId,
            ContractPdfUrl = booking.ContractPdfUrl,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
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

        booking.UpdatedAt = DateTime.UtcNow;

        await _bookingRepository.Update(booking);
        await _bookingRepository.CommitAsync();

        return new BookingResponse
        {
            BookingId = booking.BookingId,
            StudentId = booking.StudentId,
            BookingType = booking.BookingType,
            BedId = booking.BedId,
            RoomId = booking.RoomId,
            HousingUnitId = booking.HousingUnitId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            BookingStatus = booking.BookingStatus,
            IsDeleted = booking.IsDeleted,
            CommissionAmount = booking.CommissionRecord?.Amount,
            ContractId = booking.ContractId,
            ContractPdfUrl = booking.ContractPdfUrl,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
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

    public async Task<List<BookingResponse?>> CreateMultiRoomBookingAsync(MultiRoomBookingCreateRequest request)
    {
        var results = new List<BookingResponse?>();
        var contractId = Guid.NewGuid();

        foreach (var roomId in request.RoomIds)
        {
            var bookingRequest = new BookingCreateRequest
            {
                StudentId = request.StudentId,
                BookingType = BookingType.Room,
                RoomId = roomId,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            var result = await CreateBookingAsync(bookingRequest);
            if (result != null)
            {
                // Update the booking with the contract ID
                var booking = await _bookingRepository.GetAsync(result.BookingId);
                if (booking != null)
                {
                    booking.ContractId = contractId.ToString();
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _bookingRepository.Update(booking);
                    await _bookingRepository.CommitAsync();

                    result.ContractId = contractId.ToString();
                }
                results.Add(result);
            }
            else
            {
                results.Add(null);
            }
        }

        return results;
    }

    public async Task MarkBookingAsPaidAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetAll()
            .Include(b => b.CommissionRecord)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        if (booking == null)
        {
            throw new InvalidOperationException("Booking not found.");
        }

        if (booking.BookingStatus != BookingStatus.PaymentPending)
        {
            throw new InvalidOperationException("Booking is not in PaymentPending state.");
        }

        booking.BookingStatus = BookingStatus.Approved;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.Update(booking);
        await _bookingRepository.CommitAsync();
    }
}
