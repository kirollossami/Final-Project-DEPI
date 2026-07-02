using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class LandLordService : ILandLordService
{
    private readonly ILandLordRepository _landLordRepository;
    private readonly UserManager<User> _userManager;
    private readonly IFileStorageService _fileStorageService;

    public LandLordService(
        ILandLordRepository landLordRepository,
        UserManager<User> userManager,
        IFileStorageService fileStorageService)
    {
        _landLordRepository = landLordRepository;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
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
            NationalIdImageUrl = landlord.NationalIdImageUrl,
            PropertyOwnerShipProof = landlord.PropertyOwnerShipProof,
            HousingUnitDocumentationUrl = landlord.HousingUnitDocumentationUrl,
            VerificationStatus = landlord.VerificationStatus.ToString(),
            IsVerified = landlord.IsVerified,
            CreatedAt = landlord.CreatedAt,
            UpdatedAt = landlord.UpdatedAt
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
                NationalIdImageUrl = l.NationalIdImageUrl,
                PropertyOwnerShipProof = l.PropertyOwnerShipProof,
                HousingUnitDocumentationUrl = l.HousingUnitDocumentationUrl,
                VerificationStatus = l.VerificationStatus,
                IsVerified = l.IsVerified,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
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

        if (request.PropertyOwnerShipProof != null)
        {
            landlord.PropertyOwnerShipProof = request.PropertyOwnerShipProof;
        }

        await _landLordRepository.Update(landlord);
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

        await _landLordRepository.Delete(landlord);
        await _landLordRepository.CommitAsync();

        return true;
    }

    public async Task<LandLordResponse?> GetLandLordByUserIdAsync(string userId)
    {
        var landlord = await _landLordRepository.GetAll()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.UserId == userId);

        if (landlord == null) return null;

        return new LandLordResponse
        {
            LandLordId = landlord.LandLordId,
            UserId = landlord.UserId,
            CompanyName = landlord.CompanyName,
            NationalId = landlord.NationalId,
            VerificationStatus = landlord.VerificationStatus
        };
    }

    public async Task<bool> DeactivateLandLordAsync(Guid landLordId)
    {
        var landlord = await _landLordRepository.GetAll()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.LandLordId == landLordId);
        
        if (landlord == null || landlord.User == null) return false;

        landlord.User.IsActive = false;
        await _userManager.UpdateAsync(landlord.User);

        return true;
    }

    public async Task<bool> ReactivateLandLordAsync(Guid landLordId)
    {
        var landlord = await _landLordRepository.GetAll()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.LandLordId == landLordId);
        
        if (landlord == null || landlord.User == null) return false;

        landlord.User.IsActive = true;
        await _userManager.UpdateAsync(landlord.User);

        return true;
    }

    public async Task<string> GetAccountStatusAsync(string userId)
    {
        var landlord = await _landLordRepository.GetAll()
            .FirstOrDefaultAsync(l => l.UserId == userId);
        
        if (landlord == null)
            return "Not Found";
        
        return landlord.VerificationStatus;
    }

    public async Task<bool> IsLandlordVerifiedAsync(Guid landlordId)
    {
        var landlord = await _landLordRepository.GetAsync(landlordId);
        return landlord != null && landlord.IsVerified;
    }

    public async Task<bool> IsLandlordVerifiedByUserIdAsync(string userId)
    {
        var landlord = await _landLordRepository.GetAll()
            .FirstOrDefaultAsync(l => l.UserId == userId);
        return landlord != null && landlord.IsVerified;
    }

    public async Task<LandLordResponse?> CreateLandLordAsync(LandLordRegisterRequest request)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return null;

        var landlord = new Domain.Entities.LandLord
        {
            LandLordId = Guid.NewGuid(),
            UserId = user.Id,
            CompanyName = request.CompanyName,
            NationalId = request.NationalId,
            NationalIdImageUrl = string.Empty,
            PropertyOwnerShipProof = request.PropertyOwnerShipProof,
            HousingUnitDocumentationUrl = string.Empty,
            VerificationStatus = "Pending",
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _landLordRepository.Insert(landlord);
        await _landLordRepository.CommitAsync();

        return new LandLordResponse
        {
            LandLordId = landlord.LandLordId,
            UserId = landlord.UserId.ToString(),
            CompanyName = landlord.CompanyName,
            NationalId = landlord.NationalId,
            NationalIdImageUrl = landlord.NationalIdImageUrl,
            PropertyOwnerShipProof = landlord.PropertyOwnerShipProof,
            HousingUnitDocumentationUrl = landlord.HousingUnitDocumentationUrl,
            VerificationStatus = landlord.VerificationStatus.ToString(),
            IsVerified = landlord.IsVerified,
            CreatedAt = landlord.CreatedAt,
            UpdatedAt = landlord.UpdatedAt
        };
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var landlord = await _landLordRepository.GetAll()
            .FirstOrDefaultAsync(l => l.UserId == userId);

        if (landlord == null || landlord.User == null)
            throw new Exception("Landlord not found");

        var result = await _userManager.ChangePasswordAsync(landlord.User, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<LandLordResponse?> SubmitHousingUnitDocumentationAsync(
        string userId, SubmitHousingUnitDocumentationRequest request, Stream fileStream, string fileName)
    {
        var landlord = await _landLordRepository.GetAll()
            .FirstOrDefaultAsync(l => l.UserId == userId);

        if (landlord == null) return null;

        if (fileStream == null || fileStream.Length == 0) return null;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
            return null;

        var filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, "documentation");
        if (filePath == null) return null;

        landlord.HousingUnitDocumentationUrl = filePath;
        landlord.UpdatedAt = DateTime.UtcNow;
        
        // Set status to Under Review if both documents are uploaded, otherwise Pending
        if (!string.IsNullOrEmpty(landlord.NationalIdImageUrl) && !string.IsNullOrEmpty(landlord.HousingUnitDocumentationUrl))
        {
            landlord.VerificationStatus = "Under Review";
        }
        else
        {
            landlord.VerificationStatus = "Pending";
        }
        landlord.IsVerified = false;
        
        await _landLordRepository.Update(landlord);
        await _landLordRepository.CommitAsync();

        return new LandLordResponse
        {
            LandLordId = landlord.LandLordId,
            UserId = landlord.UserId.ToString(),
            CompanyName = landlord.CompanyName,
            NationalId = landlord.NationalId,
            NationalIdImageUrl = landlord.NationalIdImageUrl,
            PropertyOwnerShipProof = landlord.PropertyOwnerShipProof,
            HousingUnitDocumentationUrl = landlord.HousingUnitDocumentationUrl,
            VerificationStatus = landlord.VerificationStatus.ToString(),
            IsVerified = landlord.IsVerified,
            CreatedAt = landlord.CreatedAt,
            UpdatedAt = landlord.UpdatedAt
        };
    }

    public async Task<LandLordResponse?> UploadNationalIdAsync(string userId, Stream fileStream, string fileName)
    {
        var landlord = await _landLordRepository.GetAll()
            .FirstOrDefaultAsync(l => l.UserId == userId);

        if (landlord == null) return null;

        if (fileStream == null || fileStream.Length == 0) return null;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
            return null;

        var filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, "national-id");
        if (filePath == null) return null;

        landlord.NationalIdImageUrl = filePath;
        landlord.UpdatedAt = DateTime.UtcNow;
        
        // Set status to Under Review if both documents are uploaded, otherwise Pending
        if (!string.IsNullOrEmpty(landlord.NationalIdImageUrl) && !string.IsNullOrEmpty(landlord.HousingUnitDocumentationUrl))
        {
            landlord.VerificationStatus = "Under Review";
        }
        else
        {
            landlord.VerificationStatus = "Pending";
        }
        landlord.IsVerified = false;
        
        await _landLordRepository.Update(landlord);
        await _landLordRepository.CommitAsync();

        return new LandLordResponse
        {
            LandLordId = landlord.LandLordId,
            UserId = landlord.UserId.ToString(),
            CompanyName = landlord.CompanyName,
            NationalId = landlord.NationalId,
            NationalIdImageUrl = landlord.NationalIdImageUrl,
            PropertyOwnerShipProof = landlord.PropertyOwnerShipProof,
            HousingUnitDocumentationUrl = landlord.HousingUnitDocumentationUrl,
            VerificationStatus = landlord.VerificationStatus.ToString(),
            IsVerified = landlord.IsVerified,
            CreatedAt = landlord.CreatedAt,
            UpdatedAt = landlord.UpdatedAt
        };
    }
}
