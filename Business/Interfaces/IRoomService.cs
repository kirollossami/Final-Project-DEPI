using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IRoomService
{
    Task<RoomResponse?> GetRoomByIdAsync(Guid roomId);
    Task<RoomIndexedResponse> GetRoomsAsync(RoomFilterRequest filter);
    Task<RoomResponse?> CreateRoomAsync(RoomCreateRequest request);
    Task<RoomResponse?> UpdateRoomAsync(RoomUpdateRequest request);
    Task<bool> DeleteRoomAsync(Guid roomId);
}
