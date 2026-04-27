using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class HousingUnitService : IHousingUnitService
{
    private readonly IHousingUnitRepository _housingUnitRepository;

    public HousingUnitService(IHousingUnitRepository housingUnitRepository)
    {
        _housingUnitRepository = housingUnitRepository;
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
            UnitImageUrl = housingUnit.UnitImageUrl,
            GenderAllowed = housingUnit.GenderAllowed,
            Rules = housingUnit.Rules,
            Location = housingUnit.Location,
            NumberOfRooms = housingUnit.NumberOfRooms,
            IsAvailable = housingUnit.IsAvailable,
            AverageRating = housingUnit.AverageRating,
            ReviewCount = housingUnit.ReviewCount
        };
    }

    public async Task<HousingUnitIndexedResponse> GetHousingUnitsAsync(HousingUnitFilterRequest filter)
    {
        var query = _housingUnitRepository.GetAll().AsQueryable();

        if (filter.LandLordId.HasValue)
        {
            query = query.Where(h => h.LandLordId == filter.LandLordId.Value);
        }

        if (!string.IsNullOrEmpty(filter.City))
        {
            query = query.Where(h => h.City == filter.City);
        }

        if (!string.IsNullOrEmpty(filter.Area))
        {
            query = query.Where(h => h.Area == filter.Area);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(h => h.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(h => h.Price <= filter.MaxPrice.Value);
        }

        if (filter.GenderAllowed.HasValue)
        {
            query = query.Where(h => h.GenderAllowed == filter.GenderAllowed.Value);
        }

        if (filter.IsAvailable.HasValue)
        {
            query = query.Where(h => h.IsAvailable == filter.IsAvailable.Value);
        }

        var totalCount = await query.CountAsync();
        var housingUnits = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new HousingUnitIndexedResponse
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
                UnitImageUrl = h.UnitImageUrl,
                GenderAllowed = h.GenderAllowed,
                Rules = h.Rules,
                Location = h.Location,
                NumberOfRooms = h.NumberOfRooms,
                IsAvailable = h.IsAvailable,
                AverageRating = h.AverageRating,
                ReviewCount = h.ReviewCount
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<HousingUnitResponse?> CreateHousingUnitAsync(HousingUnitCreateRequest request)
    {
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
            UnitImageUrl = request.UnitImageUrl,
            GenderAllowed = request.GenderAllowed,
            Rules = request.Rules,
            Location = request.Location,
            NumberOfRooms = request.NumberOfRooms,
            IsAvailable = request.IsAvailable,
            AverageRating = 0,
            ReviewCount = 0
        };

        await _housingUnitRepository.Insert(housingUnit);
        await _housingUnitRepository.CommitAsync();

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
            UnitImageUrl = housingUnit.UnitImageUrl,
            GenderAllowed = housingUnit.GenderAllowed,
            Rules = housingUnit.Rules,
            Location = housingUnit.Location,
            NumberOfRooms = housingUnit.NumberOfRooms,
            IsAvailable = housingUnit.IsAvailable,
            AverageRating = housingUnit.AverageRating,
            ReviewCount = housingUnit.ReviewCount
        };
    }

    public async Task<HousingUnitResponse?> UpdateHousingUnitAsync(HousingUnitUpdateRequest request)
    {
        var housingUnit = await _housingUnitRepository.GetAsync(request.HousingUnitId);
        if (housingUnit == null) return null;

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

        if (request.UnitImageUrl != null)
        {
            housingUnit.UnitImageUrl = request.UnitImageUrl;
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

        if (request.NumberOfRooms.HasValue)
        {
            housingUnit.NumberOfRooms = request.NumberOfRooms.Value;
        }

        if (request.IsAvailable.HasValue)
        {
            housingUnit.IsAvailable = request.IsAvailable.Value;
        }

        _housingUnitRepository.Update(housingUnit);
        await _housingUnitRepository.CommitAsync();

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
            UnitImageUrl = housingUnit.UnitImageUrl,
            GenderAllowed = housingUnit.GenderAllowed,
            Rules = housingUnit.Rules,
            Location = housingUnit.Location,
            NumberOfRooms = housingUnit.NumberOfRooms,
            IsAvailable = housingUnit.IsAvailable,
            AverageRating = housingUnit.AverageRating,
            ReviewCount = housingUnit.ReviewCount
        };
    }

    public async Task<bool> DeleteHousingUnitAsync(Guid housingUnitId)
    {
        var housingUnit = await _housingUnitRepository.GetAsync(housingUnitId);
        if (housingUnit == null) return false;

        _housingUnitRepository.Delete(housingUnit);
        await _housingUnitRepository.CommitAsync();

        return true;
    }
}
