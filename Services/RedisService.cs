using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using PlataformaCreditos.ViewModels;
using StackExchange.Redis;

namespace PlataformaCreditos.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _redis;
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _cacheExpiry;

    public RedisService(IConnectionMultiplexer redis, IDistributedCache cache, IOptions<RedisSettings> redisSettings)
    {
        _redis = redis.GetDatabase();
        _cache = cache;
        _cacheExpiry = TimeSpan.FromSeconds(redisSettings.Value.CacheExpirationSeconds);
    }

    // Session: track last visited application
    public async Task SetLastSolicitudIdAsync(string userId, int? solicitudId)
    {
        if (solicitudId.HasValue)
        {
            await _redis.StringSetAsync($"last_solicitud:{userId}", solicitudId.Value.ToString());
        }
        else
        {
            await _redis.KeyDeleteAsync($"last_solicitud:{userId}");
        }
    }

    public async Task<int?> GetLastSolicitudIdAsync(string userId)
    {
        var value = await _redis.StringGetAsync($"last_solicitud:{userId}");
        if (string.IsNullOrEmpty(value)) return null;
        if (int.TryParse((string)value, out var id)) return id;
        return null;
    }

    // Cache: get/cache application list
    public async Task<List<SolicitudListViewModel>> GetCachedSolicitudesAsync(
        string cacheKey,
        Func<Task<List<SolicitudListViewModel>>> fetchFunc,
        TimeSpan? expiry = null)
    {
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<SolicitudListViewModel>>(cached) ?? new List<SolicitudListViewModel>();
        }

        var data = await fetchFunc();
        await _cache.SetStringAsync(cacheKey,
            System.Text.Json.JsonSerializer.Serialize(data),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry ?? _cacheExpiry });
        return data;
    }

    public async Task RemoveCachedSolicitudesAsync(string cacheKey)
    {
        await _cache.RemoveAsync(cacheKey);
    }
}

public class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int CacheExpirationSeconds { get; set; } = 60;
}
