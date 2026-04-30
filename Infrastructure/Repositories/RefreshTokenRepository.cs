using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken?> GetValidTokenAsync(string token);
    Task RevokeAllUserTokensAsync(string userId);
}

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(StudentHousingDBContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await GetAll()
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<RefreshToken?> GetValidTokenAsync(string token)
    {
        return await GetAll()
            .FirstOrDefaultAsync(rt => 
                rt.Token == token && 
                !rt.IsRevoked && 
                rt.ExpiryDate > DateTime.UtcNow);
    }

    public async Task RevokeAllUserTokensAsync(string userId)
    {
        var tokens = await GetAll()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await Update(token);
        }
    }
}
