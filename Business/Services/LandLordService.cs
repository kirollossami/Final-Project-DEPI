using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class LandLordService : ILandLordService
{
    private readonly ILandLordRepository _landLordRepository;

    public LandLordService(ILandLordRepository landLordRepository)
    {
        _landLordRepository = landLordRepository;
    }

    public async Task<LandLordResponse?> GetLandLordByIdAsync(Guid landLordId)
    {
        var landlord = await _landLordRepository.GetAll()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.LandLordId == landLordId);

        if (landlord == null) return null;

        return new LandLordResponse
        {
            LandLordId = landlord.LandLordId,
            UserId = landlord.UserId.ToString(),
            CompanyName = landlord.CompanyName,
            NationalId = landlord.NationalId,
            VerificationStatus = landlord.VerificationStatus.ToString()
        };
    }

    public async Task<LandLordIndexedResponse> GetLandLordsAsync(LandLordFilterRequest filter)
    {
        var query = _landLordRepository.GetAll().Include(l => l.User).AsQueryable();

        if (!string.IsNullOrEmpty(filter.CompanyName))
        {
            query = query.Where(l => l.CompanyName.Contains(filter.CompanyName));
        }

        if (!string.IsNullOrEmpty(filter.VerificationStatus))
        {
            query = query.Where(l => l.VerificationStatus == filter.VerificationStatus);
        }

        var totalCount = await query.CountAsync();
        var landlords = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new LandLordIndexedResponse
        {
            Records = landlords.Select(l => new LandLordResponse
            {
                LandLordId = l.LandLordId,
                UserId = l.UserId,
                CompanyName = l.CompanyName,
                NationalId = l.NationalId,
                VerificationStatus = l.VerificationStatus
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<LandLordResponse?> CreateLandLordAsync(LandLordRegisterRequest request)
    {
        var landlord = new Domain.Entities.LandLord
        {
            LandLordId = Guid.NewGuid(),
            UserId = Guid.NewGuid().ToString(),
            CompanyName = request.CompanyName,
            NationalId = request.NationalId,
            PropertyOwnerShipProof = request.PropertyOwnerShipProof,
            VerificationStatus = "Pending"
        };

        await _landLordRepository.Insert(landlord);
        await _landLordRepository.CommitAsync();

        return new LandLordResponse
        {
            LandLordId = landlord.LandLordId,
            UserId = landlord.UserId.ToString(),
            CompanyName = landlord.CompanyName,
            NationalId = landlord.NationalId,
            VerificationStatus = landlord.VerificationStatus.ToString()
        };
    }

    public async Task<LandLordResponse?> UpdateLandLordAsync(UpdateLandLordRequest request)
    {
        var landlord = await _landLordRepository.GetAll()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.LandLordId == request.LandLordId);

        if (landlord == null) return null;

        if (request.CompanyName != null)
        {
            landlord.CompanyName = request.CompanyName;
        }

        _landLordRepository.Update(landlord);
        await _landLordRepository.CommitAsync();

        return new LandLordResponse
        {
            LandLordId = landlord.LandLordId,
            UserId = landlord.UserId.ToString(),
            CompanyName = landlord.CompanyName,
            NationalId = landlord.NationalId,
            VerificationStatus = landlord.VerificationStatus.ToString()
        };
    }

    public async Task<bool> DeleteLandLordAsync(Guid landLordId)
    {
        var landlord = await _landLordRepository.GetAsync(landLordId);
        if (landlord == null) return false;

        _landLordRepository.Delete(landlord);
        await _landLordRepository.CommitAsync();

        return true;
    }
}
