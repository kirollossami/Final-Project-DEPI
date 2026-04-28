using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;

    public WishlistService(IWishlistRepository wishlistRepository)
    {
        _wishlistRepository = wishlistRepository;
    }

    public async Task<WishlistResponse?> GetWishlistByIdAsync(Guid wishlistId)
    {
        var wishlist = await _wishlistRepository.GetAsync(wishlistId);
        if (wishlist == null) return null;

        return new WishlistResponse
        {
            WishlistId = wishlist.WishlistId,
            StudentId = wishlist.StudentId,
            HousingUnitId = wishlist.HousingUnitId,
            AddedDate = wishlist.AddedDate
        };
    }

    public async Task<WishlistIndexedResponse> GetWishlistsAsync(WishlistFilterRequest filter)
    {
        var query = _wishlistRepository.GetAll().AsQueryable();

        if (filter.StudentId.HasValue)
        {
            query = query.Where(w => w.StudentId == filter.StudentId.Value);
        }

        if (filter.HousingUnitId.HasValue)
        {
            query = query.Where(w => w.HousingUnitId == filter.HousingUnitId.Value);
        }

        var totalCount = await query.CountAsync();
        var wishlists = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new WishlistIndexedResponse
        {
            Records = wishlists.Select(w => new WishlistResponse
            {
                WishlistId = w.WishlistId,
                StudentId = w.StudentId,
                HousingUnitId = w.HousingUnitId,
                AddedDate = w.AddedDate
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<WishlistResponse?> AddToWishlistAsync(WishlistCreateRequest request)
    {
        var wishlist = new Domain.Entities.Wishlist
        {
            WishlistId = Guid.NewGuid(),
            StudentId = request.StudentId,
            HousingUnitId = request.HousingUnitId,
            AddedDate = DateTime.UtcNow
        };

        await _wishlistRepository.Insert(wishlist);
        await _wishlistRepository.CommitAsync();

        return new WishlistResponse
        {
            WishlistId = wishlist.WishlistId,
            StudentId = wishlist.StudentId,
            HousingUnitId = wishlist.HousingUnitId,
            AddedDate = wishlist.AddedDate
        };
    }

    public async Task<bool> RemoveFromWishlistAsync(Guid wishlistId)
    {
        var wishlist = await _wishlistRepository.GetAsync(wishlistId);
        if (wishlist == null) return false;

        await _wishlistRepository.Delete(wishlist);
        await _wishlistRepository.CommitAsync();

        return true;
    }
}
