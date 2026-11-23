using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Dal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Geocoding;

/// <summary>
/// Проставляет координаты аптекам без них. Неудачные адреса повторяет не чаще,
/// чем раз в Geocoding:RetryFailedAfter, чтобы не долбить геокодер.
/// </summary>
public sealed class StoreGeocodingService(
    DrugsBotDbContext db,
    IGeocoder geocoder,
    IOptions<GeocodingOptions> options,
    ILogger<StoreGeocodingService> logger)
{
    public async Task<int> GeocodePendingAsync(CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.Enabled) return 0;
        if (string.IsNullOrWhiteSpace(settings.UserAgent))
        {
            logger.LogWarning("Геокодирование пропущено: не задан Geocoding:UserAgent (обязателен для Nominatim)");
            return 0;
        }

        var retryBefore = DateTime.UtcNow - settings.RetryFailedAfter;
        var stores = await db.DrugStores
            .Where(s => s.Latitude == null && (s.GeocodedAt == null || s.GeocodedAt < retryBefore))
            .OrderBy(s => s.GeocodedAt)
            .Take(settings.BatchSize)
            .ToListAsync(ct);

        var found = 0;
        foreach (var store in stores)
        {
            try
            {
                var point = await geocoder.GeocodeAsync(store.Address.City, store.Address.Street, store.Address.House, ct);
                store.SetLocation(point?.Lat, point?.Lng);
                if (point is not null) found++;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                logger.LogWarning("Геокодер недоступен, продолжим в следующий проход: {Message}", ex.Message);
                break;
            }
        }

        await db.SaveChangesAsync(ct);
        if (stores.Count > 0)
            logger.LogInformation("Геокодирование: найдено {Found} из {Total} аптек", found, stores.Count);
        return found;
    }
}
