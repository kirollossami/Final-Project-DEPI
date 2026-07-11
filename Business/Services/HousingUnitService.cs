using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shared.Cache;
using Shared.Cache.Helper;

namespace Business.Services;

public class HousingUnitService : IHousingUnitService
{
    private readonly IHousingUnitRepository _housingUnitRepository;
    private readonly ILandLordService _landLordService;
    private readonly ICacheService _cache;

    public HousingUnitService(IHousingUnitRepository housingUnitRepository, ILandLordService landLordService, ICacheService cache)
    {
        _housingUnitRepository = housingUnitRepository;
        _landLordService = landLordService;
        _cache = cache;
    }

    public async Task<HousingUnitResponse?> GetHousingUnitByIdAsync(Guid housingUnitId)
    {
        var housingUnit = await _housingUnitRepository.GetAsync(housingUnitId);
        if (housingUnit == null) return null;

        return new HousingUnitResponse
        {
            HousingUnitId = housingUnit.HousingUnitId,
            LandLordId = housingUnit.LandLordId,
            Title = housingUnit.Title,
            Description = housingUnit.Description,
            Address = housingUnit.Address,
            City = housingUnit.City,
            Area = housingUnit.Area,
            Price = housingUnit.Price,
            BaseMonthlyPrice = housingUnit.BaseMonthlyPrice,
            UnitImageUrl = housingUnit.UnitImageUrl,
            VideoUrl = housingUnit.VideoUrl,
            GenderAllowed = housingUnit.GenderAllowed,
            Rules = housingUnit.Rules,
            Location = housingUnit.Location,
            Latitude = housingUnit.Latitude,
            Longitude = housingUnit.Longitude,
            NumberOfRooms = housingUnit.NumberOfRooms,
            IsAvailable = housingUnit.IsAvailable,
            AverageRating = housingUnit.AverageRating,
            ReviewCount = housingUnit.ReviewCount,
            IsDeleted = housingUnit.IsDeleted,
            CreatedAt = housingUnit.CreatedAt,
            UpdatedAt = housingUnit.UpdatedAt
        };
    }

    public async Task<List<MapPinResponse>> GetMapPinsAsync()
    {

        if (_cache.TryGet(CacheHelper.HousingUnitsMapPinsCacheKey, out List<MapPinResponse>? cachedPins))
        {
            return cachedPins;
        }

        var units = await _housingUnitRepository.GetAll()
            .Where(h => !h.IsDeleted)
            .Select(h => new MapPinResponse
            {
                HousingUnitId = h.HousingUnitId,
                Title = h.Title,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                Price = h.Price,
                IsAvailable = h.IsAvailable
            })
            .ToListAsync();

        _cache.Set(CacheHelper.HousingUnitsMapPinsCacheKey, units);

        return units;
    }

    public async Task<HousingUnitIndexedResponse> GetHousingUnitsAsync(HousingUnitFilterRequest filter)
    {
        var hasNoFilters = !filter.LandLordId.HasValue
            && string.IsNullOrEmpty(filter.City)
            && string.IsNullOrEmpty(filter.Area)
            && !filter.MinPrice.HasValue
            && !filter.MaxPrice.HasValue
            && !filter.GenderAllowed.HasValue
            && !filter.IsAvailable.HasValue;

        if (hasNoFilters && filter.PageNumber == 1 && filter.PageSize == 10)
        {
            if (_cache.TryGet(CacheHelper.StudentHousingCacheKey, out HousingUnitIndexedResponse? cached))
                return cached;
        }

        var query = _housingUnitRepository.GetAll().AsQueryable();

        if (filter.LandLordId.HasValue)
            query = query.Where(h => h.LandLordId == filter.LandLordId.Value);

        if (!string.IsNullOrEmpty(filter.City))
            query = query.Where(h => h.City == filter.City);

        if (!string.IsNullOrEmpty(filter.Area))
            query = query.Where(h => h.Area == filter.Area);

        if (filter.MinPrice.HasValue)
            query = query.Where(h => h.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(h => h.Price <= filter.MaxPrice.Value);

        if (filter.GenderAllowed.HasValue)
            query = query.Where(h => h.GenderAllowed == filter.GenderAllowed.Value);

        if (filter.IsAvailable.HasValue)
            query = query.Where(h => h.IsAvailable == filter.IsAvailable.Value);

        var totalCount = await query.CountAsync();
        var housingUnits = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var response = new HousingUnitIndexedResponse
        {
            Records = housingUnits.Select(h => new HousingUnitResponse
            {
                HousingUnitId = h.HousingUnitId,
                LandLordId = h.LandLordId,
                Title = h.Title,
                Description = h.Description,
                Address = h.Address,
                City = h.City,
                Area = h.Area,
                Price = h.Price,
                BaseMonthlyPrice = h.BaseMonthlyPrice,
                UnitImageUrl = h.UnitImageUrl,
                VideoUrl = h.VideoUrl,
                GenderAllowed = h.GenderAllowed,
                Rules = h.Rules,
                Location = h.Location,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                NumberOfRooms = h.NumberOfRooms,
                IsAvailable = h.IsAvailable,
                AverageRating = h.AverageRating,
                ReviewCount = h.ReviewCount,
                IsDeleted = h.IsDeleted,
                CreatedAt = h.CreatedAt,
                UpdatedAt = h.UpdatedAt
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };

        if (hasNoFilters && filter.PageNumber == 1 && filter.PageSize == 10)
            _cache.Set(CacheHelper.StudentHousingCacheKey, response);

        return response;
    }

    public async Task<HousingUnitResponse?> CreateHousingUnitAsync(HousingUnitCreateRequest request)
    {
        try
        {
            // Check if landlord is verified
            var isVerified = await _landLordService.IsLandlordVerifiedAsync(request.LandLordId);
            if (!isVerified)
            {
                return null;
            }

            var housingUnit = new Domain.Entities.HousingUnit
            {
                HousingUnitId = Guid.NewGuid(),
                LandLordId = request.LandLordId,
                Title = request.Title,
                Description = request.Description,
                Address = request.Address,
                City = request.City,
                Area = request.Area,
                Price = request.Price,
                BaseMonthlyPrice = request.BaseMonthlyPrice,
                UnitImageUrl = request.UnitImageUrl,
                VideoUrl = request.VideoUrl,
                GenderAllowed = request.GenderAllowed,
                Rules = request.Rules,
                Location = request.Location,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                NumberOfRooms = request.NumberOfRooms,
                IsAvailable = request.IsAvailable,
                AverageRating = 0,
                ReviewCount = 0,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await _housingUnitRepository.Insert(housingUnit);
            await _housingUnitRepository.CommitAsync();

            _cache.Remove(CacheHelper.StudentHousingCacheKey); // Invalidate cache

            return new HousingUnitResponse
            {
                HousingUnitId = housingUnit.HousingUnitId,
                LandLordId = housingUnit.LandLordId,
                Title = housingUnit.Title,
                Description = housingUnit.Description,
                Address = housingUnit.Address,
                City = housingUnit.City,
                Area = housingUnit.Area,
                Price = housingUnit.Price,
                BaseMonthlyPrice = housingUnit.BaseMonthlyPrice,
                UnitImageUrl = housingUnit.UnitImageUrl,
                VideoUrl = housingUnit.VideoUrl,
                GenderAllowed = housingUnit.GenderAllowed,
                Rules = housingUnit.Rules,
                Location = housingUnit.Location,
                Latitude = housingUnit.Latitude,
                Longitude = housingUnit.Longitude,
                NumberOfRooms = housingUnit.NumberOfRooms,
                IsAvailable = housingUnit.IsAvailable,
                AverageRating = housingUnit.AverageRating,
                ReviewCount = housingUnit.ReviewCount,
                IsDeleted = housingUnit.IsDeleted,
                CreatedAt = housingUnit.CreatedAt,
                UpdatedAt = housingUnit.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? "No inner exception";
            throw new Exception($"Error creating housing unit: {ex.Message}. Inner: {innerMessage}", ex);
        }
    }

    public async Task<HousingUnitResponse?> UpdateHousingUnitAsync(HousingUnitUpdateRequest request)
    {
        var housingUnit = await _housingUnitRepository.GetAsync(request.HousingUnitId);
        if (housingUnit == null) return null;

        // Check if landlord is verified
        var isVerified = await _landLordService.IsLandlordVerifiedAsync(housingUnit.LandLordId);
        if (!isVerified)
        {
            return null;
        }

        if (request.Title != null)
        {
            housingUnit.Title = request.Title;
        }

        if (request.Description != null)
        {
            housingUnit.Description = request.Description;
        }

        if (request.Address != null)
        {
            housingUnit.Address = request.Address;
        }

        if (request.City != null)
        {
            housingUnit.City = request.City;
        }

        if (request.Area != null)
        {
            housingUnit.Area = request.Area;
        }

        if (request.Price.HasValue)
        {
            housingUnit.Price = request.Price.Value;
        }

        if (request.BaseMonthlyPrice.HasValue)
        {
            housingUnit.BaseMonthlyPrice = request.BaseMonthlyPrice.Value;
        }

        if (request.UnitImageUrl != null)
        {
            housingUnit.UnitImageUrl = request.UnitImageUrl;
        }

        if (request.VideoUrl != null)
        {
            housingUnit.VideoUrl = request.VideoUrl;
        }

        if (request.GenderAllowed.HasValue)
        {
            housingUnit.GenderAllowed = request.GenderAllowed.Value;
        }

        if (request.Rules != null)
        {
            housingUnit.Rules = request.Rules;
        }

        if (request.Location != null)
        {
            housingUnit.Location = request.Location;
        }

        if (request.Latitude.HasValue)
        {
            housingUnit.Latitude = request.Latitude.Value;
        }

        if (request.Longitude.HasValue)
        {
            housingUnit.Longitude = request.Longitude.Value;
        }

        if (request.NumberOfRooms.HasValue)
        {
            housingUnit.NumberOfRooms = request.NumberOfRooms.Value;
        }

        if (request.IsAvailable.HasValue)
        {
            housingUnit.IsAvailable = request.IsAvailable.Value;
        }

        housingUnit.UpdatedAt = DateTime.UtcNow;

        _housingUnitRepository.Update(housingUnit);
        await _housingUnitRepository.CommitAsync();

        _cache.Remove(CacheHelper.StudentHousingCacheKey); // Invalidate cache


        return new HousingUnitResponse
        {
            HousingUnitId = housingUnit.HousingUnitId,
            LandLordId = housingUnit.LandLordId,
            Title = housingUnit.Title,
            Description = housingUnit.Description,
            Address = housingUnit.Address,
            City = housingUnit.City,
            Area = housingUnit.Area,
            Price = housingUnit.Price,
            BaseMonthlyPrice = housingUnit.BaseMonthlyPrice,
            UnitImageUrl = housingUnit.UnitImageUrl,
            VideoUrl = housingUnit.VideoUrl,
            GenderAllowed = housingUnit.GenderAllowed,
            Rules = housingUnit.Rules,
            Location = housingUnit.Location,
            Latitude = housingUnit.Latitude,
            Longitude = housingUnit.Longitude,
            NumberOfRooms = housingUnit.NumberOfRooms,
            IsAvailable = housingUnit.IsAvailable,
            AverageRating = housingUnit.AverageRating,
            ReviewCount = housingUnit.ReviewCount,
            IsDeleted = housingUnit.IsDeleted,
            CreatedAt = housingUnit.CreatedAt,
            UpdatedAt = housingUnit.UpdatedAt
        };
    }

    public async Task<bool> DeleteHousingUnitAsync(Guid housingUnitId)
    {
        var housingUnit = await _housingUnitRepository.GetAsync(housingUnitId);
        if (housingUnit == null) return false;

        var isVerified = await _landLordService.IsLandlordVerifiedAsync(housingUnit.LandLordId);
        if (!isVerified)
        {
            return false;
        }

        await _housingUnitRepository.Delete(housingUnit);
        await _housingUnitRepository.CommitAsync();

        _cache.Remove(CacheHelper.StudentHousingCacheKey); // Invalidate cache


        return true;
    }

    public async Task<HousingUnitDetailsResponse?> GetHousingUnitDetailsAsync(Guid housingUnitId)
    {
        var housingUnit = await _housingUnitRepository.GetAll()
            .Include(h => h.Rooms)
            .Include(h => h.UnitImages)
            .Include(h => h.Reviews)
            .FirstOrDefaultAsync(h => h.HousingUnitId == housingUnitId);

        if (housingUnit == null) return null;

        return new HousingUnitDetailsResponse
        {
            HousingUnitId = housingUnit.HousingUnitId,
            LandLordId = housingUnit.LandLordId,
            Title = housingUnit.Title,
            Description = housingUnit.Description,
            Address = housingUnit.Address,
            City = housingUnit.City,
            Area = housingUnit.Area,
            Price = housingUnit.Price,
            BaseMonthlyPrice = housingUnit.BaseMonthlyPrice,
            UnitImageUrl = housingUnit.UnitImageUrl,
            VideoUrl = housingUnit.VideoUrl,
            GenderAllowed = housingUnit.GenderAllowed,
            Rules = housingUnit.Rules,
            IsDeleted = housingUnit.IsDeleted,
            AverageRating = housingUnit.AverageRating,
            ReviewCount = housingUnit.ReviewCount,
            Location = housingUnit.Location,
            Latitude = housingUnit.Latitude,
            Longitude = housingUnit.Longitude,
            NumberOfRooms = housingUnit.NumberOfRooms,
            IsAvailable = housingUnit.IsAvailable,
            CreatedAt = housingUnit.CreatedAt,
            UpdatedAt = housingUnit.UpdatedAt,
            Rooms = housingUnit.Rooms?.Select(r => new RoomResponse
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
                IsAvailable = r.IsAvailable
            }).ToList() ?? new List<RoomResponse>(),
            UnitImages = housingUnit.UnitImages?.OrderBy(ui => ui.DisplayOrder).Select(ui => new UnitImageResponse
            {
                UnitImageId = ui.UnitImageId,
                HousingUnitId = ui.HousingUnitId,
                ImageUrl = ui.ImageUrl,
                Description = ui.Description,
                DisplayOrder = ui.DisplayOrder,
                UploadedAt = ui.UploadedAt
            }).ToList() ?? new List<UnitImageResponse>(),
            Reviews = housingUnit.Reviews?.Select(r => new ReviewResponse
            {
                ReviewId = r.ReviewId,
                StudentId = r.StudentId,
                HousingUnitId = r.HousingUnitId,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewDate = r.ReviewDate
            }).ToList() ?? new List<ReviewResponse>()
        };
    }
}
