using Microsoft.Extensions.Caching.Memory;
using Shared.Cache.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Cache;

public interface ICacheService
{
    bool TryGet<T>(string key, out T? value);
    void Set<T>(string key, T item, MemoryCacheEntryOptions? options = null);
    void Remove(string key);
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public CacheService (IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryGet<T>(string key, out T? value)
    {
        return _cache.TryGetValue(key, out value);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    public void Set<T>(string key, T item, MemoryCacheEntryOptions? options = null)
    {
        _cache.Set(key, item, options ?? GetDefaultOptions());
    }


    private MemoryCacheEntryOptions GetDefaultOptions()
    {
        return new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromDays(CacheHelper.ExpiresTime))
            .SetPriority(CacheItemPriority.Normal)
            .SetSize(1);
    }


}
