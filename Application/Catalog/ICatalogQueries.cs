namespace Application.Catalog;

/// <summary>Сторона чтения каталога для веб-клиента.</summary>
public interface ICatalogQueries
{
    Task<SearchResponseDto> SearchAsync(SearchQuery query, CancellationToken ct);
    Task<IReadOnlyList<SuggestionDto>> SuggestAsync(string q, int limit, CancellationToken ct);
    Task<IReadOnlyList<DrugSummaryDto>> GetPopularAsync(int limit, CancellationToken ct);
    Task<DrugDetailsDto?> GetDrugAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<AnalogDto>> GetAnalogsAsync(Guid id, int limit, CancellationToken ct);
    Task<PharmacyListDto> ListPharmaciesAsync(string? network, string? city, string? q, CancellationToken ct);
    Task<PharmacyDetailsDto?> GetPharmacyAsync(Guid id, CancellationToken ct);
    Task<CatalogStatsDto> GetStatsAsync(CancellationToken ct);
}
