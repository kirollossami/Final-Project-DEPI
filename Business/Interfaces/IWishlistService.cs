using Business.DTOs.Requests;
using Business.DTOs.Responses;

namespace Business.Interfaces;

public interface IWishlistService
{
    Task<WishlistResponse?> GetWishlistByIdAsync(Guid wishlistId);
    Task<WishlistIndexedResponse> GetWishlistsAsync(WishlistFilterRequest filter);
    Task<WishlistResponse?> AddToWishlistAsync(WishlistCreateRequest request);
    Task<bool> RemoveFromWishlistAsync(Guid wishlistId);
}
