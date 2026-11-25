using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Geocoding;

/// <summary>
/// Геокодер на OpenStreetMap Nominatim. Соблюдает политику сервиса: не чаще 1 запроса в секунду,
/// осмысленный User-Agent (Geocoding:UserAgent). Для больших объёмов — свой экземпляр Nominatim.
/// </summary>
public sealed class NominatimGeocoder(HttpClient http, IOptions<GeocodingOptions> options, ILogger<NominatimGeocoder> logger)
    : IGeocoder
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTime _lastRequestAt = DateTime.MinValue;

    public async Task<GeoPoint?> GeocodeAsync(string city, string street, string house, CancellationToken ct)
    {
        // «б/н» (без номера) геокодер не понимает и из-за него не находит даже улицу
        var query = house.Trim().Equals("б/н", StringComparison.OrdinalIgnoreCase)
            ? $"{street}, {city}"
            : $"{street} {house}, {city}";
        await Gate.WaitAsync(ct);
        try
        {
            var wait = _lastRequestAt + options.Value.MinInterval - DateTime.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
            _lastRequestAt = DateTime.UtcNow;

            var url = $"search?format=jsonv2&limit=1&accept-language=ru&q={Uri.EscapeDataString(query)}";
            var results = await http.GetFromJsonAsync<List<NominatimResult>>(url, ct);
            var first = results?.FirstOrDefault();
            if (first is null
                || !double.TryParse(first.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !double.TryParse(first.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            {
                logger.LogInformation("Адрес не найден геокодером: {Query}", query);
                return null;
            }
            return new GeoPoint(lat, lng);
        }
        finally
        {
            Gate.Release();
        }
    }

    private sealed record NominatimResult(
        [property: JsonPropertyName("lat")] string Lat,
        [property: JsonPropertyName("lon")] string Lon);
}
