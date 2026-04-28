using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface ILandLordService
{
    Task<LandLordResponse?> GetLandLordByIdAsync(Guid landLordId);
    Task<LandLordResponse?> GetLandLordByUserIdAsync(string userId);
    Task<LandLordIndexedResponse> GetLandLordsAsync(LandLordFilterRequest filter);
    Task<LandLordResponse?> UpdateLandLordAsync(UpdateLandLordRequest request);
    Task<bool> DeleteLandLordAsync(Guid landLordId);
    Task<bool> DeactivateLandLordAsync(Guid landLordId);
    Task<bool> ReactivateLandLordAsync(Guid landLordId);
    Task<bool> ValidateNationalIdAsync(string nationalId);
}
