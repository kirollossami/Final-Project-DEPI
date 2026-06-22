using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Domain.Enums;

namespace Business.Interfaces;

public interface ILandLordService
{
    Task<LandLordResponse?> GetLandLordByIdAsync(Guid landLordId);
    Task<LandLordResponse?> GetLandLordByUserIdAsync(string userId);
    Task<LandLordIndexedResponse> GetLandLordsAsync(LandLordFilterRequest filter);
    Task<LandLordResponse?> CreateLandLordAsync(LandLordRegisterRequest request);
    Task<LandLordResponse?> UpdateLandLordAsync(UpdateLandLordRequest request);
    Task<bool> DeleteLandLordAsync(Guid landLordId);
    Task<bool> DeactivateLandLordAsync(Guid landLordId);
    Task<bool> ReactivateLandLordAsync(Guid landLordId);
    Task<string> GetAccountStatusAsync(string userId);
    Task<bool> IsLandlordVerifiedAsync(Guid landlordId);
    Task<bool> IsLandlordVerifiedByUserIdAsync(string userId);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<LandLordResponse?> SubmitHousingUnitDocumentationAsync(string userId, SubmitHousingUnitDocumentationRequest request, Stream fileStream, string fileName);
    Task<LandLordResponse?> UploadNationalIdAsync(string userId, Stream fileStream, string fileName);
}
