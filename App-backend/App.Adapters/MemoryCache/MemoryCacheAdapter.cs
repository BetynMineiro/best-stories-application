using App.Domain.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace App.Adapters.MemoryCache;

public class MemoryCacheAdapter : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheAdapter> _logger;

    public MemoryCacheAdapter(IMemoryCache cache, ILogger<MemoryCacheAdapter> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? absoluteTtl = null) where T : class
    {
        if (_cache.TryGetValue(key, out T? cached))
        {
            _logger.LogDebug("Cache hit for key {Key}", key);
            return cached;
        }

        _logger.LogDebug("Cache miss for key {Key}", key);
        var value = await factory().ConfigureAwait(false);
        if (value == null)
            return null;

        var options = new MemoryCacheEntryOptions();
        if (absoluteTtl.HasValue)
            options.AbsoluteExpirationRelativeToNow = absoluteTtl.Value;

        _cache.Set(key, value, options);
        return value;
    }
}
