using System.Text.RegularExpressions;
using Application.Catalog;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Dal.Queries;

/// <summary>
/// Запросы каталога для веб-клиента. Агрегаты (мин./макс. цена, число аптек) считаются в SQL,
/// сущности не загружаются целиком.
/// </summary>
public sealed partial class CatalogQueries(
    DrugsBotDbContext db,
    ISemanticSearchClient semantic,
    IOptions<SemanticSearchOptions> semanticOptions,
    IOptions<CatalogOptions> catalogOptions) : ICatalogQueries
{
    /// <summary>Предложения, которые парсер видел позже этого момента, считаются актуальными.</summary>
    private readonly DateTime _freshSince = DateTime.UtcNow - catalogOptions.Value.OfferMaxAge;

    /// <summary>Сколько кандидатов запрашивать у векторного поиска (фильтры и пагинация — поверх них).</summary>
    private const int SemanticCandidates = 100;

    #region Поиск

    public async Task<SearchResponseDto> SearchAsync(SearchQuery query, CancellationToken ct)
    {
        var q = query.Q.Trim();
        return query.Mode == SearchMode.Semantic && q.Length > 0
            ? await SemanticSearchAsync(query with { Q = q }, ct)
            : await ExactSearchAsync(query with { Q = q }, ct);
    }

    private async Task<SearchResponseDto> ExactSearchAsync(SearchQuery query, CancellationToken ct)
    {
        var q = query.Q;
        var matched = MatchByText(db.Drugs.AsNoTracking(), q);

        var facets = await FacetsAsync(matched, ct);
        var filtered = ApplyFilters(matched, query);
        var total = await filtered.CountAsync(ct);

        var rows = Summaries(filtered, q);
        rows = query.Sort switch
        {
            SearchSort.PriceAsc => rows.OrderBy(r => r.MinPrice == null).ThenBy(r => r.MinPrice).ThenBy(r => r.Name),
            SearchSort.PriceDesc => rows.OrderBy(r => r.MinPrice == null).ThenByDescending(r => r.MinPrice).ThenBy(r => r.Name),
            SearchSort.Name => rows.OrderBy(r => r.Name),
            _ when q.Length == 0 => rows.OrderBy(r => r.Name),
            _ => rows.OrderByDescending(r => r.IsPrefix).ThenByDescending(r => r.Similarity).ThenBy(r => r.Name),
        };

        var page = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        var items = page.Select(r => ToSearchItem(r, q.Length == 0 ? null : r.IsPrefix ? 1.0 : Math.Round(r.Similarity, 3))).ToList();
        return new SearchResponseDto(query.Q, "exact", total, query.Page, query.PageSize, items, facets);
    }

    private async Task<SearchResponseDto> SemanticSearchAsync(SearchQuery query, CancellationToken ct)
    {
        var settings = semanticOptions.Value;
        var hits = (await semantic.SearchAsync(query.Q, SemanticCandidates, ct)).Where(h => h.Score >= settings.MinScore).ToList();
        var scores = hits.GroupBy(h => h.Id).ToDictionary(g => g.Key, g => g.Max(h => h.Score));
        var ids = scores.Keys.ToList();

        var candidates = await Summaries(db.Drugs.AsNoTracking().Where(d => ids.Contains(d.Id)), "").ToListAsync(ct);

        // Гибрид: векторный поиск даёт кандидатов, а оставляем те, где есть слово из запроса,
        // либо те, что почти не уступают лучшему результату.
        var stems = QueryStems(query.Q);
        bool Lexical(SummaryRow r) => stems.Count > 0 && stems.Any(st =>
            r.Name.Contains(st, StringComparison.OrdinalIgnoreCase)
            || (r.ActiveIngredient?.Contains(st, StringComparison.OrdinalIgnoreCase) ?? false));
        var top = scores.Count > 0 ? scores.Values.Max() : 0;
        var lexical = candidates.Where(Lexical).Select(r => r.Id).ToHashSet();
        var rows = candidates.Where(r => lexical.Contains(r.Id) || scores[r.Id] >= top - settings.RelativeWindow).ToList();
        var facets = new SearchFacetsDto(
            rows.GroupBy(r => r.Manufacturer)
                .Select(g => new FacetDto(g.Key, g.Key, g.Count()))
                .OrderByDescending(f => f.Count).ThenBy(f => f.Label).ToList(),
            rows.Where(r => r.CountryCode != null && r.CountryName != null).GroupBy(r => (r.CountryCode, r.CountryName))
                .Select(g => new FacetDto(g.Key.CountryCode!, g.Key.CountryName!, g.Count()))
                .OrderByDescending(f => f.Count).ThenBy(f => f.Label).ToList());

        var filtered = rows
            .Where(r => query.Manufacturer is null || r.Manufacturer == query.Manufacturer)
            .Where(r => query.Country is null || r.CountryCode == query.Country);
        filtered = query.Sort switch
        {
            SearchSort.PriceAsc => filtered.OrderBy(r => r.MinPrice ?? decimal.MaxValue),
            SearchSort.PriceDesc => filtered.OrderByDescending(r => r.MinPrice ?? -1),
            SearchSort.Name => filtered.OrderBy(r => r.Name, StringComparer.Create(new("ru-RU"), true)),
            _ => filtered.OrderByDescending(r => lexical.Contains(r.Id)).ThenByDescending(r => scores[r.Id]),
        };
        var list = filtered.ToList();
        var items = list.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(r => ToSearchItem(r, Math.Round(Math.Clamp(scores[r.Id], 0, 1), 3)))
            .ToList();
        return new SearchResponseDto(query.Q, "semantic", list.Count, query.Page, query.PageSize, items, facets);
    }

    public async Task<IReadOnlyList<SuggestionDto>> SuggestAsync(string q, int limit, CancellationToken ct)
    {
        q = q.Trim();
        if (q.Length < 2) return [];
        return await Summaries(MatchByText(db.Drugs.AsNoTracking(), q), q)
            .OrderByDescending(r => r.IsPrefix).ThenByDescending(r => r.Similarity).ThenBy(r => r.Name)
            .Take(limit)
            .Select(r => new SuggestionDto(r.Id, r.Name, r.Manufacturer, r.MinPrice))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DrugSummaryDto>> GetPopularAsync(int limit, CancellationToken ct)
    {
        // Данных о популярности запросов пока нет — показываем лекарства (с известным действующим веществом),
        // которые продаются в наибольшем числе аптек; косметика и товары без вещества идут после.
        var rows = await Summaries(db.Drugs.AsNoTracking(), "")
            .Where(r => r.PharmacyCount > 0)
            .OrderByDescending(r => r.ActiveIngredient != null)
            .ThenByDescending(r => r.PharmacyCount)
            .ThenBy(r => r.MinPrice)
            .ThenBy(r => r.Name)
            .Take(limit)
            .ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    #endregion

    #region Препарат

    public async Task<DrugDetailsDto?> GetDrugAsync(Guid id, CancellationToken ct)
    {
        // Описание живёт в векторном индексе — запрашиваем параллельно с БД.
        var documentTask = semantic.GetDocumentAsync(id, ct);

        var row = await Summaries(db.Drugs.AsNoTracking().Where(d => d.Id == id), "").FirstOrDefaultAsync(ct);
        if (row is null) return null;

        var offers = await db.DrugItems.AsNoTracking()
            .Where(i => i.DrugId == id && i.SeenAt >= _freshSince)
            .OrderBy(i => i.Cost)
            .Select(i => new { i.Cost, i.UpdatedAt, Store = i.DrugStore! })
            .ToListAsync(ct);

        var document = await documentTask;
        return new DrugDetailsDto
        {
            Id = row.Id,
            Name = row.Name,
            Manufacturer = row.Manufacturer,
            Country = CountryOf(row),
            Form = row.Form,
            ActiveIngredient = row.ActiveIngredient,
            MinPrice = row.MinPrice,
            MaxPrice = row.MaxPrice,
            PharmacyCount = row.PharmacyCount,
            UpdatedAt = AsUtc(row.UpdatedAt),
            Description = string.IsNullOrWhiteSpace(document?.Description) ? null : document.Description,
            DescriptionSource = string.IsNullOrWhiteSpace(document?.Description) ? null : document.DescriptionSource ?? "generated",
            Offers = offers.Select(o => new OfferDto
            {
                Pharmacy = ToPharmacy(o.Store),
                Price = o.Cost,
                Quantity = null, // парсер не знает реального остатка
                UpdatedAt = AsUtc(o.UpdatedAt),
            }).ToList(),
        };
    }

    /// <summary>
    /// Аналоги: сначала препараты с тем же действующим веществом (надёжно, по данным источника),
    /// затем — близкие по векторному индексу. Если векторный поиск недоступен, отдаём хотя бы аналоги по веществу.
    /// </summary>
    public async Task<IReadOnlyList<AnalogDto>> GetAnalogsAsync(Guid id, int limit, CancellationToken ct)
    {
        var ingredient = await db.Drugs.AsNoTracking().Where(d => d.Id == id).Select(d => d.ActiveIngredient).FirstOrDefaultAsync(ct);

        var sameIngredient = new List<SummaryRow>();
        if (!string.IsNullOrWhiteSpace(ingredient))
        {
            var normalized = ingredient.ToLower();
            sameIngredient = await Summaries(db.Drugs.AsNoTracking()
                    .Where(d => d.Id != id && d.ActiveIngredient != null && d.ActiveIngredient.ToLower() == normalized), "")
                .Where(r => r.PharmacyCount > 0)
                .OrderByDescending(r => r.PharmacyCount).ThenBy(r => r.MinPrice)
                .Take(limit)
                .ToListAsync(ct);
        }
        var result = sameIngredient.Select(r => ToAnalog(r, 1.0)).ToList();
        if (result.Count >= limit) return result;

        IReadOnlyList<SemanticHit> hits;
        try
        {
            hits = await semantic.SimilarAsync(id, limit * 2 + 1, ct);
        }
        catch (SemanticSearchUnavailableException) when (result.Count > 0)
        {
            return result;
        }

        var minSimilarity = semanticOptions.Value.MinSimilarity;
        var taken = result.Select(r => r.Id).Append(id).ToHashSet();
        var candidates = hits.Where(h => !taken.Contains(h.Id) && h.Score >= minSimilarity).ToList();
        if (candidates.Count == 0) return result;

        var ids = candidates.Select(h => h.Id).ToList();
        var rows = (await Summaries(db.Drugs.AsNoTracking().Where(d => ids.Contains(d.Id)), "").ToListAsync(ct))
            .Where(r => r.PharmacyCount > 0)
            .ToDictionary(r => r.Id);
        result.AddRange(candidates.Where(h => rows.ContainsKey(h.Id))
            .Take(limit - result.Count)
            .Select(h => ToAnalog(rows[h.Id], Math.Round(Math.Clamp(h.Score, 0, 1), 3))));
        return result;
    }

    private static AnalogDto ToAnalog(SummaryRow r, double similarity) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Manufacturer = r.Manufacturer,
        Country = CountryOf(r),
        Form = r.Form,
        ActiveIngredient = r.ActiveIngredient,
        MinPrice = r.MinPrice,
        MaxPrice = r.MaxPrice,
        PharmacyCount = r.PharmacyCount,
        UpdatedAt = AsUtc(r.UpdatedAt),
        Similarity = similarity,
    };

    #endregion

    #region Аптеки

    public async Task<PharmacyListDto> ListPharmaciesAsync(string? network, string? city, string? q, CancellationToken ct)
    {
        var since = _freshSince;
        var stores = db.DrugStores.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(network)) stores = stores.Where(s => s.DrugNetwork == network);
        if (!string.IsNullOrWhiteSpace(city)) stores = stores.Where(s => s.Address.City == city);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{EscapeLike(q.Trim())}%";
            stores = stores.Where(s => EF.Functions.ILike(s.DrugNetwork, pattern)
                                       || EF.Functions.ILike(s.Address.City, pattern)
                                       || EF.Functions.ILike(s.Address.Street, pattern));
        }

        var items = await stores
            .OrderBy(s => s.Address.City).ThenBy(s => s.DrugNetwork).ThenBy(s => s.Number)
            .Select(s => new { Store = s, Assortment = s.DrugItems.Count(i => i.SeenAt >= since) })
            .ToListAsync(ct);
        var networks = await db.DrugStores.Select(s => s.DrugNetwork).Distinct().OrderBy(n => n).ToListAsync(ct);
        var cities = await db.DrugStores.Select(s => s.Address.City).Distinct().OrderBy(c => c).ToListAsync(ct);

        return new PharmacyListDto(
            items.Select(i => ToPharmacyListItem(i.Store, i.Assortment)).ToList(),
            networks,
            cities);
    }

    public async Task<PharmacyDetailsDto?> GetPharmacyAsync(Guid id, CancellationToken ct)
    {
        var store = await db.DrugStores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (store is null) return null;

        var offers = await db.DrugItems.AsNoTracking()
            .Where(i => i.DrugStoreId == id && i.SeenAt >= _freshSince)
            .OrderBy(i => i.Drug!.Name)
            .Select(i => new { i.DrugId, i.Drug!.Name, i.Drug.Manufacturer, i.Drug.Form, i.Cost, i.UpdatedAt })
            .ToListAsync(ct);

        var p = ToPharmacy(store);
        return new PharmacyDetailsDto
        {
            Id = p.Id,
            Network = p.Network,
            Number = p.Number,
            City = p.City,
            Address = p.Address,
            Phone = p.Phone,
            Hours = p.Hours,
            Location = p.Location,
            Offers = offers.Select(o => new PharmacyOfferDto(
                new PharmacyOfferDrugDto(o.DrugId, o.Name, o.Manufacturer, o.Form), o.Cost, AsUtc(o.UpdatedAt))).ToList(),
        };
    }

    public async Task<CatalogStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var drugs = await db.Drugs.CountAsync(ct);
        var pharmacies = await db.DrugStores.CountAsync(ct);
        var networks = await db.DrugStores.Select(s => s.DrugNetwork).Distinct().CountAsync(ct);
        var fresh = db.DrugItems.Where(i => i.SeenAt >= _freshSince);
        var offers = await fresh.CountAsync(ct);
        var updatedAt = await fresh.MaxAsync(i => (DateTime?)i.UpdatedAt, ct);
        return new CatalogStatsDto(drugs, pharmacies, networks, offers, updatedAt is null ? null : AsUtc(updatedAt.Value));
    }

    #endregion

    #region Вспомогательное

    private sealed class SummaryRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
        public string Manufacturer { get; init; } = "";
        public string? CountryCode { get; init; }
        public string? CountryName { get; init; }
        public string? Form { get; init; }
        public string? ActiveIngredient { get; init; }
        public decimal? MinPrice { get; init; }
        public decimal? MaxPrice { get; init; }
        public int PharmacyCount { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public bool IsPrefix { get; init; }
        public double Similarity { get; init; }
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "от", "при", "для", "после", "против", "средство", "лекарство", "таблетки", "что", "как", "чем", "лечить",
    };

    /// <summary>Основы значимых слов запроса: «от изжоги» → «изжо», «от кашля» → «кашл», «давление» → «давлен».</summary>
    private static List<string> QueryStems(string q) =>
        WordPattern().Matches(q.ToLowerInvariant().Replace('ё', 'е'))
            .Select(m => m.Value)
            .Where(w => w.Length >= 5 && !StopWords.Contains(w)) // «боль» совпадала бы с «больного»
            .Select(w => w[..Math.Max(4, w.Length - 2)])
            .Distinct()
            .ToList();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex WordPattern();

    /// <summary>Совпадение по подстроке в названии/производителе или по триграммному сходству слов (опечатки).</summary>
    private static IQueryable<Drug> MatchByText(IQueryable<Drug> drugs, string q)
    {
        if (q.Length == 0) return drugs;
        var pattern = $"%{EscapeLike(q)}%";
        return drugs.Where(d => EF.Functions.ILike(d.Name, pattern)
                                || EF.Functions.ILike(d.Manufacturer, pattern)
                                || (d.ActiveIngredient != null && EF.Functions.ILike(d.ActiveIngredient, pattern))
                                || EF.Functions.TrigramsAreWordSimilar(q, d.Name));
    }

    private IQueryable<SummaryRow> Summaries(IQueryable<Drug> drugs, string q)
    {
        var prefix = $"{EscapeLike(q)}%";
        var since = _freshSince;
        return drugs.Select(d => new SummaryRow
        {
            Id = d.Id,
            Name = d.Name,
            Manufacturer = d.Manufacturer,
            CountryCode = d.CountryCodeId,
            CountryName = d.Country != null ? d.Country.Name : null,
            Form = d.Form,
            ActiveIngredient = d.ActiveIngredient,
            MinPrice = d.DrugItems.Where(i => i.SeenAt >= since).Min(i => (decimal?)i.Cost),
            MaxPrice = d.DrugItems.Where(i => i.SeenAt >= since).Max(i => (decimal?)i.Cost),
            PharmacyCount = d.DrugItems.Count(i => i.SeenAt >= since),
            UpdatedAt = d.DrugItems.Where(i => i.SeenAt >= since).Max(i => (DateTime?)i.UpdatedAt),
            IsPrefix = q != "" && EF.Functions.ILike(d.Name, prefix),
            Similarity = q != "" ? EF.Functions.TrigramsWordSimilarity(q, d.Name) : 0,
        });
    }

    private static IQueryable<Drug> ApplyFilters(IQueryable<Drug> drugs, SearchQuery query)
    {
        if (!string.IsNullOrEmpty(query.Manufacturer)) drugs = drugs.Where(d => d.Manufacturer == query.Manufacturer);
        if (!string.IsNullOrEmpty(query.Country)) drugs = drugs.Where(d => d.CountryCodeId == query.Country);
        return drugs;
    }

    /// <summary>Фасеты считаются по всей выдаче запроса до применения фильтров.</summary>
    private static async Task<SearchFacetsDto> FacetsAsync(IQueryable<Drug> matched, CancellationToken ct)
    {
        var manufacturers = await matched.GroupBy(d => d.Manufacturer)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count).ThenBy(g => g.Key)
            .Take(50)
            .ToListAsync(ct);
        var countries = await matched.Where(d => d.CountryCodeId != null)
            .GroupBy(d => new { d.CountryCodeId, Name = d.Country != null ? d.Country.Name : d.CountryCodeId })
            .Select(g => new { g.Key.CountryCodeId, g.Key.Name, Count = g.Count() })
            .OrderByDescending(g => g.Count).ThenBy(g => g.Name)
            .ToListAsync(ct);
        return new SearchFacetsDto(
            manufacturers.Select(m => new FacetDto(m.Key, m.Key, m.Count)).ToList(),
            countries.Select(c => new FacetDto(c.CountryCodeId!, c.Name!, c.Count)).ToList());
    }

    private static CountryDto? CountryOf(SummaryRow r) =>
        r.CountryCode is null || r.CountryName is null ? null : new CountryDto(r.CountryCode, r.CountryName);

    private static DrugSummaryDto ToSummary(SummaryRow r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Manufacturer = r.Manufacturer,
        Country = CountryOf(r),
        Form = r.Form,
        ActiveIngredient = r.ActiveIngredient,
        MinPrice = r.MinPrice,
        MaxPrice = r.MaxPrice,
        PharmacyCount = r.PharmacyCount,
        UpdatedAt = AsUtc(r.UpdatedAt),
    };

    private static SearchResultItemDto ToSearchItem(SummaryRow r, double? score) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Manufacturer = r.Manufacturer,
        Country = CountryOf(r),
        Form = r.Form,
        ActiveIngredient = r.ActiveIngredient,
        MinPrice = r.MinPrice,
        MaxPrice = r.MaxPrice,
        PharmacyCount = r.PharmacyCount,
        UpdatedAt = AsUtc(r.UpdatedAt),
        Score = score,
    };

    private static PharmacyDto ToPharmacy(DrugStore s) => new()
    {
        Id = s.Id,
        Network = s.DrugNetwork,
        Number = s.Number,
        City = s.Address.City,
        Address = FormatAddress(s.Address.Street, s.Address.House),
        Phone = CleanPhone(s.PhoneNumber),
        Hours = s.Hours,
        Location = s.Latitude is { } lat && s.Longitude is { } lng ? new GeoPointDto(lat, lng) : null,
    };

    private static PharmacyListItemDto ToPharmacyListItem(DrugStore s, int assortment)
    {
        var p = ToPharmacy(s);
        return new PharmacyListItemDto
        {
            Id = p.Id, Network = p.Network, Number = p.Number, City = p.City, Address = p.Address,
            Phone = p.Phone, Hours = p.Hours, Location = p.Location, AssortmentCount = assortment,
        };
    }

    /// <summary>Парсер иногда оставляет номер дома в названии улицы — не дублируем его.</summary>
    private static string FormatAddress(string street, string house)
    {
        street = street.Trim().TrimEnd(',');
        house = house.Trim();
        if (house.Length == 0 || street.EndsWith(" " + house, StringComparison.OrdinalIgnoreCase)) return street;
        return $"{street}, {house}";
    }

    private static string? CleanPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var cleaned = PhonePrefix().Replace(phone, "").Trim();
        return cleaned.Length == 0 ? null : cleaned;
    }

    [GeneratedRegex(@"^\s*тел\.?\s*:?\s*", RegexOptions.IgnoreCase)]
    private static partial Regex PhonePrefix();

    private static string EscapeLike(string s) =>
        s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>Npgsql отдаёт timestamptz как UTC; защищаемся от Unspecified, чтобы JSON содержал «Z».</summary>
    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? AsUtc(DateTime? value) => value is null ? null : AsUtc(value.Value);

    #endregion
}
