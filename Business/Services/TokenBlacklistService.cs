using System.Collections.Concurrent;

namespace Business.Services;

public interface ITokenBlacklistService
{
    bool IsTokenBlacklisted(string token);
    void BlacklistToken(string token);
    void RemoveToken(string token);
}

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();

    public bool IsTokenBlacklisted(string token)
    {
        return _blacklistedTokens.ContainsKey(token);
    }

    public void BlacklistToken(string token)
    {
        _blacklistedTokens.TryAdd(token, DateTime.UtcNow);
    }

    public void RemoveToken(string token)
    {
        _blacklistedTokens.TryRemove(token, out _);
    }
}
