namespace Application.Catalog;

// Модели чтения для веб-клиента. Формат JSON (camelCase) — контракт DrugsEngine.Web: lib/contracts.ts, docs/API.md.

public sealed record CountryDto(string Code, string Name);

public sealed record GeoPointDto(double Lat, double Lng);

public class PharmacyDto
{
    public required Guid Id { get; init; }
    public required string Network { get; init; }
    public required int Number { get; init; }
    public required string City { get; init; }
    public required string Address { get; init; }
    public string? Phone { get; init; }
    public string? Hours { get; init; }
    public GeoPointDto? Location { get; init; }
}

public class DrugSummaryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Manufacturer { get; init; }
    public CountryDto? Country { get; init; }
    /// <summary>Форма выпуска — в модели пока нет.</summary>
    public string? Form { get; init; }
    /// <summary>Действующее вещество — в модели пока нет.</summary>
    public string? ActiveIngredient { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int PharmacyCount { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class SearchResultItemDto : DrugSummaryDto
{
    public double? Score { get; init; }
}

public sealed class AnalogDto : DrugSummaryDto
{
    public required double Similarity { get; init; }
}

public sealed class OfferDto
{
    public required PharmacyDto Pharmacy { get; init; }
    public required decimal Price { get; init; }
    public double? Quantity { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

public sealed class DrugDetailsDto : DrugSummaryDto
{
    public string? Description { get; init; }
    /// <summary>"official" | "generated" | null.</summary>
    public string? DescriptionSource { get; init; }
    public required IReadOnlyList<OfferDto> Offers { get; init; }
}

public sealed record FacetDto(string Value, string Label, int Count);

public sealed record SearchFacetsDto(IReadOnlyList<FacetDto> Manufacturers, IReadOnlyList<FacetDto> Countries);

public sealed record SearchResponseDto(
    string Query,
    string Mode,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<SearchResultItemDto> Items,
    SearchFacetsDto Facets);

public sealed record SuggestionDto(Guid Id, string Name, string Manufacturer, decimal? MinPrice);

public sealed record ItemsDto<T>(IReadOnlyList<T> Items);

public sealed class PharmacyListItemDto : PharmacyDto
{
    public required int AssortmentCount { get; init; }
}

public sealed record PharmacyListDto(IReadOnlyList<PharmacyListItemDto> Items, IReadOnlyList<string> Networks, IReadOnlyList<string> Cities);

public sealed record PharmacyOfferDrugDto(Guid Id, string Name, string Manufacturer, string? Form);

public sealed record PharmacyOfferDto(PharmacyOfferDrugDto Drug, decimal Price, DateTime UpdatedAt);

public sealed class PharmacyDetailsDto : PharmacyDto
{
    public required IReadOnlyList<PharmacyOfferDto> Offers { get; init; }
}

public sealed record CatalogStatsDto(int Drugs, int Pharmacies, int Networks, int Offers, DateTime? UpdatedAt);

public enum SearchMode { Exact, Semantic }

public enum SearchSort { Relevance, PriceAsc, PriceDesc, Name }

public sealed record SearchQuery(
    string Q,
    SearchMode Mode,
    SearchSort Sort,
    string? Manufacturer,
    string? Country,
    int Page,
    int PageSize);
