using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class BedService : IBedService
{
    private readonly IBedRepository _bedRepository;
    private readonly IRoomRepository _roomRepository;

    public BedService(
        IBedRepository bedRepository,
        IRoomRepository roomRepository)
    {
        _bedRepository = bedRepository;
        _roomRepository = roomRepository;
    }

    public async Task<BedResponse?> GetBedByIdAsync(Guid bedId)
    {
        var bed = await _bedRepository.GetAll()
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.BedId == bedId);

        if (bed == null) return null;

        return new BedResponse
        {
            BedId = bed.BedId,
            RoomId = bed.RoomId,
            BedNumber = bed.BedNumber,
            IsAvailable = bed.IsAvailable,
            IsOccupied = bed.IsOccupied,
            CreatedAt = bed.CreatedAt,
            UpdatedAt = bed.UpdatedAt
        };
    }

    public async Task<BedIndexedResponse> GetBedsAsync(BedFilterRequest filter)
    {
        var query = _bedRepository.GetAll()
            .Include(b => b.Room)
            .AsQueryable();

        if (filter.RoomId.HasValue)
        {
            query = query.Where(b => b.RoomId == filter.RoomId.Value);
        }

        if (filter.IsAvailable.HasValue)
        {
            query = query.Where(b => b.IsAvailable == filter.IsAvailable.Value);
        }

        var totalCount = await query.CountAsync();
        var beds = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new BedIndexedResponse
        {
            Records = beds.Select(b => new BedResponse
            {
                BedId = b.BedId,
                RoomId = b.RoomId,
                BedNumber = b.BedNumber,
                IsAvailable = b.IsAvailable,
                IsOccupied = b.IsOccupied,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<BedResponse?> CreateBedAsync(BedCreateRequest request)
    {
        var room = await _roomRepository.GetAsync(request.RoomId);
        if (room == null) return null;

        var bed = new Bed
        {
            BedId = Guid.NewGuid(),
            RoomId = request.RoomId,
            BedNumber = request.BedNumber,
            IsAvailable = true,
            IsOccupied = false,
            CreatedAt = DateTime.UtcNow
        };

        await _bedRepository.Insert(bed);
        await _bedRepository.CommitAsync();

        return new BedResponse
        {
            BedId = bed.BedId,
            RoomId = bed.RoomId,
            BedNumber = bed.BedNumber,
            IsAvailable = bed.IsAvailable,
            IsOccupied = bed.IsOccupied,
            CreatedAt = bed.CreatedAt,
            UpdatedAt = bed.UpdatedAt
        };
    }

    public async Task<BedResponse?> UpdateBedAsync(BedUpdateRequest request)
    {
        var bed = await _bedRepository.GetAsync(request.BedId);
        if (bed == null) return null;

        if (!string.IsNullOrEmpty(request.BedNumber))
        {
            bed.BedNumber = request.BedNumber;
        }

        if (request.IsAvailable.HasValue)
        {
            bed.IsAvailable = request.IsAvailable.Value;
        }

        if (request.IsOccupied.HasValue)
        {
            bed.IsOccupied = request.IsOccupied.Value;
        }

        bed.UpdatedAt = DateTime.UtcNow;

        await _bedRepository.Update(bed);
        await _bedRepository.CommitAsync();

        return new BedResponse
        {
            BedId = bed.BedId,
            RoomId = bed.RoomId,
            BedNumber = bed.BedNumber,
            IsAvailable = bed.IsAvailable,
            IsOccupied = bed.IsOccupied,
            CreatedAt = bed.CreatedAt,
            UpdatedAt = bed.UpdatedAt
        };
    }

    public async Task<bool> DeleteBedAsync(Guid bedId)
    {
        var bed = await _bedRepository.GetAsync(bedId);
        if (bed == null) return false;

        await _bedRepository.Delete(bed);
        await _bedRepository.CommitAsync();

        return true;
    }

    public async Task<List<BedResponse>> GetBedsByRoomIdAsync(Guid roomId)
    {
        var beds = await _bedRepository.GetAll()
            .Where(b => b.RoomId == roomId)
            .ToListAsync();

        return beds.Select(b => new BedResponse
        {
            BedId = b.BedId,
            RoomId = b.RoomId,
            BedNumber = b.BedNumber,
            IsAvailable = b.IsAvailable,
            IsOccupied = b.IsOccupied,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        }).ToList();
    }
}
