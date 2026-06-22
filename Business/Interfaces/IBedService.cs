    using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IBedService
{
    Task<BedResponse?> GetBedByIdAsync(Guid bedId);
    Task<BedIndexedResponse> GetBedsAsync(BedFilterRequest filter);
    Task<BedResponse?> CreateBedAsync(BedCreateRequest request);
    Task<BedResponse?> UpdateBedAsync(BedUpdateRequest request);
    Task<bool> DeleteBedAsync(Guid bedId);
    Task<List<BedResponse>> GetBedsByRoomIdAsync(Guid roomId);
}
