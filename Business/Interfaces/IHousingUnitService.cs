using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IHousingUnitService
{
    Task<HousingUnitResponse?> GetHousingUnitByIdAsync(Guid housingUnitId);
    Task<HousingUnitIndexedResponse> GetHousingUnitsAsync(HousingUnitFilterRequest filter);
    Task<HousingUnitResponse?> CreateHousingUnitAsync(HousingUnitCreateRequest request);
    Task<HousingUnitResponse?> UpdateHousingUnitAsync(HousingUnitUpdateRequest request);
    Task<bool> DeleteHousingUnitAsync(Guid housingUnitId);
}
