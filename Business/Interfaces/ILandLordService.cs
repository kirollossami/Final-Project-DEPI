using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface ILandLordService
{
    Task<LandLordResponse?> GetLandLordByIdAsync(Guid landLordId);
    Task<LandLordIndexedResponse> GetLandLordsAsync(LandLordFilterRequest filter);
    Task<LandLordResponse?> CreateLandLordAsync(LandLordRegisterRequest request);
    Task<LandLordResponse?> UpdateLandLordAsync(UpdateLandLordRequest request);
    Task<bool> DeleteLandLordAsync(Guid landLordId);
}
