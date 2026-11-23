namespace Application.Interfaces;

public sealed record GeoPoint(double Lat, double Lng);

/// <summary>Преобразует адрес в координаты.</summary>
public interface IGeocoder
{
    /// <returns>Координаты или null, если адрес не найден.</returns>
    Task<GeoPoint?> GeocodeAsync(string city, string street, string house, CancellationToken ct);
}
