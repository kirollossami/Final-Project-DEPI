using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IHousingUnitRepository _housingUnitRepository;
    private readonly ILandLordService _landLordService;

    public RoomService(
        IRoomRepository roomRepository,
        IHousingUnitRepository housingUnitRepository,
        ILandLordService landLordService)
    {
        _roomRepository = roomRepository;
        _housingUnitRepository = housingUnitRepository;
        _landLordService = landLordService;
    }

    public async Task<RoomResponse?> GetRoomByIdAsync(Guid roomId)
    {
        var room = await _roomRepository.GetAll()
            .Include(r => r.Beds)
            .FirstOrDefaultAsync(r => r.RoomId == roomId);
        if (room == null) return null;

        return new RoomResponse
        {
            RoomId = room.RoomId,
            HousingUnitId = room.HousingUnitId,
            RoomType = room.RoomType,
            RoomImageUrl = room.RoomImageUrl,
            NumberOfBeds = room.NumberOfBeds,
            Price = room.Price,
            PricePerMonth = room.PricePerMonth,
            Capacity = room.Capacity,
            CurrentOccupancy = room.CurrentOccupancy,
            IsAvailable = room.IsAvailable,
            CalculatedPrice = null,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt,
            Beds = room.Beds?.Select(b => new BedResponse
            {
                BedId = b.BedId,
                RoomId = b.RoomId,
                BedNumber = b.BedNumber,
                IsAvailable = b.IsAvailable,
                IsOccupied = b.IsOccupied,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList() ?? new List<BedResponse>()
        };
    }

    public async Task<RoomIndexedResponse> GetRoomsAsync(RoomFilterRequest filter)
    {
        var query = _roomRepository.GetAll().AsQueryable();

        if (filter.HousingUnitId.HasValue)
        {
            query = query.Where(r => r.HousingUnitId == filter.HousingUnitId.Value);
        }

        if (filter.RoomType.HasValue)
        {
            query = query.Where(r => r.RoomType == filter.RoomType.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(r => r.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(r => r.Price <= filter.MaxPrice.Value);
        }

        if (filter.IsAvailable.HasValue)
        {
            query = query.Where(r => r.IsAvailable == filter.IsAvailable.Value);
        }

        var totalCount = await query.CountAsync();
        var rooms = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new RoomIndexedResponse
        {
            Records = rooms.Select(r => new RoomResponse
            {
                RoomId = r.RoomId,
                HousingUnitId = r.HousingUnitId,
                RoomType = r.RoomType,
                RoomImageUrl = r.RoomImageUrl,
                NumberOfBeds = r.NumberOfBeds,
                Price = r.Price,
                PricePerMonth = r.PricePerMonth,
                Capacity = r.Capacity,
                CurrentOccupancy = r.CurrentOccupancy,
                IsAvailable = r.IsAvailable,
                CalculatedPrice = null,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                Beds = new List<BedResponse>()
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<RoomResponse?> CreateRoomAsync(RoomCreateRequest request)
    {
        var housingUnit = await _housingUnitRepository.GetAsync(request.HousingUnitId);
        if (housingUnit == null) return null;

        var isVerified = await _landLordService.IsLandlordVerifiedAsync(housingUnit.LandLordId);
        if (!isVerified) return null;

        var room = new Domain.Entities.Room
        {
            RoomId = Guid.NewGuid(),
            HousingUnitId = request.HousingUnitId,
            RoomType = request.RoomType,
            RoomImageUrl = request.RoomImageUrl,
            NumberOfBeds = request.NumberOfBeds,
            Price = request.Price,
            PricePerMonth = request.Price,
            Capacity = request.Capacity,
            CurrentOccupancy = 0,
            IsAvailable = request.IsAvailable,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _roomRepository.Insert(room);
        await _roomRepository.CommitAsync();

        return new RoomResponse
        {
            RoomId = room.RoomId,
            HousingUnitId = room.HousingUnitId,
            RoomType = room.RoomType,
            RoomImageUrl = room.RoomImageUrl,
            NumberOfBeds = room.NumberOfBeds,
            Price = room.Price,
            PricePerMonth = room.PricePerMonth,
            Capacity = room.Capacity,
            CurrentOccupancy = room.CurrentOccupancy,
            IsAvailable = room.IsAvailable,
            CalculatedPrice = null,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt,
            Beds = new List<BedResponse>()
        };
    }

    public async Task<RoomResponse?> UpdateRoomAsync(RoomUpdateRequest request)
    {
        var room = await _roomRepository.GetAsync(request.RoomId);
        if (room == null) return null;

        var housingUnit = await _housingUnitRepository.GetAsync(room.HousingUnitId);
        if (housingUnit == null) return null;

        var isVerified = await _landLordService.IsLandlordVerifiedAsync(housingUnit.LandLordId);
        if (!isVerified) return null;

        if (request.RoomType.HasValue)
        {
            room.RoomType = request.RoomType.Value;
        }

        if (request.RoomImageUrl != null)
        {
            room.RoomImageUrl = request.RoomImageUrl;
        }

        if (request.NumberOfBeds.HasValue)
        {
            room.NumberOfBeds = request.NumberOfBeds.Value;
        }

        if (request.Price.HasValue)
        {
            room.Price = request.Price.Value;
            room.PricePerMonth = request.Price.Value;
        }

        if (request.Capacity.HasValue)
        {
            room.Capacity = request.Capacity.Value;
        }

        if (request.IsAvailable.HasValue)
        {
            room.IsAvailable = request.IsAvailable.Value;
        }

        room.UpdatedAt = DateTime.UtcNow;

        await _roomRepository.Update(room);
        await _roomRepository.CommitAsync();

        return new RoomResponse
        {
            RoomId = room.RoomId,
            HousingUnitId = room.HousingUnitId,
            RoomType = room.RoomType,
            RoomImageUrl = room.RoomImageUrl,
            NumberOfBeds = room.NumberOfBeds,
            Price = room.Price,
            PricePerMonth = room.PricePerMonth,
            Capacity = room.Capacity,
            CurrentOccupancy = room.CurrentOccupancy,
            IsAvailable = room.IsAvailable,
            CalculatedPrice = null,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt,
            Beds = new List<BedResponse>()
        };
    }

    public async Task<bool> DeleteRoomAsync(Guid roomId)
    {
        var room = await _roomRepository.GetAsync(roomId);
        if (room == null) return false;

        var housingUnit = await _housingUnitRepository.GetAsync(room.HousingUnitId);
        if (housingUnit == null) return false;

        var isVerified = await _landLordService.IsLandlordVerifiedAsync(housingUnit.LandLordId);
        if (!isVerified) return false;

        await _roomRepository.Delete(room);
        await _roomRepository.CommitAsync();

        return true;
    }
}
