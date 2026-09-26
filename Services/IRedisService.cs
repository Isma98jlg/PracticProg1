using PlataformaCreditos.ViewModels;
using StackExchange.Redis;

namespace PlataformaCreditos.Services;

public interface IRedisService
{
    // Session: track last visited application
    Task SetLastSolicitudIdAsync(string userId, int? solicitudId);
    Task<int?> GetLastSolicitudIdAsync(string userId);

    // Cache: get/cache application list
    Task<List<SolicitudListViewModel>> GetCachedSolicitudesAsync(string cacheKey, Func<Task<List<SolicitudListViewModel>>> fetchFunc, TimeSpan? expiry = null);
    Task RemoveCachedSolicitudesAsync(string cacheKey);
}
