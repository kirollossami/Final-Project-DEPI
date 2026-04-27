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

    public RoomService(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<RoomResponse?> GetRoomByIdAsync(Guid roomId)
    {
        var room = await _roomRepository.GetAsync(roomId);
        if (room == null) return null;

        return new RoomResponse
        {
            RoomId = room.RoomId,
            HousingUnitId = room.HousingUnitId,
            RoomType = room.RoomType,
            RoomImageUrl = room.RoomImageUrl,
            NumberOfBeds = room.NumberOfBeds,
            Price = room.Price,
            IsAvailable = room.IsAvailable
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
                IsAvailable = r.IsAvailable
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<RoomResponse?> CreateRoomAsync(RoomCreateRequest request)
    {
        var room = new Domain.Entities.Room
        {
            RoomId = Guid.NewGuid(),
            HousingUnitId = request.HousingUnitId,
            RoomType = request.RoomType,
            RoomImageUrl = request.RoomImageUrl,
            NumberOfBeds = request.NumberOfBeds,
            Price = request.Price,
            IsAvailable = request.IsAvailable
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
            IsAvailable = room.IsAvailable
        };
    }

    public async Task<RoomResponse?> UpdateRoomAsync(RoomUpdateRequest request)
    {
        var room = await _roomRepository.GetAsync(request.RoomId);
        if (room == null) return null;

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
        }

        if (request.IsAvailable.HasValue)
        {
            room.IsAvailable = request.IsAvailable.Value;
        }

        _roomRepository.Update(room);
        await _roomRepository.CommitAsync();

        return new RoomResponse
        {
            RoomId = room.RoomId,
            HousingUnitId = room.HousingUnitId,
            RoomType = room.RoomType,
            RoomImageUrl = room.RoomImageUrl,
            NumberOfBeds = room.NumberOfBeds,
            Price = room.Price,
            IsAvailable = room.IsAvailable
        };
    }

    public async Task<bool> DeleteRoomAsync(Guid roomId)
    {
        var room = await _roomRepository.GetAsync(roomId);
        if (room == null) return false;

        _roomRepository.Delete(room);
        await _roomRepository.CommitAsync();

        return true;
    }
}
