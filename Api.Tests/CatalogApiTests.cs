using System.Net;
using System.Text.Json;
using Application.Interfaces;
using Xunit;

namespace Api.Tests;

[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;

/// <summary>
/// Проверяет API v1 по контракту веб-клиента (DrugsEngine.Web/lib/contracts.ts) на реальном PostgreSQL.
/// Данные — scripts/dev-seed.sql.
/// </summary>
[Collection(nameof(ApiCollection))]
public sealed class CatalogApiTests(ApiFactory factory)
{
    private static readonly Guid Ibuprofen = Guid.Parse("d0a1b2c3-0000-4000-8000-000000000001");
    private static readonly Guid Nurofen = Guid.Parse("d0a1b2c3-0000-4000-8000-000000000002");
    private static readonly Guid Mig = Guid.Parse("d0a1b2c3-0000-4000-8000-000000000003");
    private static readonly Guid Omeprazole = Guid.Parse("d0a1b2c3-0000-4000-8000-000000000009");
    private static readonly Guid Omez = Guid.Parse("d0a1b2c3-0000-4000-8000-000000000010");
    private static readonly Guid NoOffers = Guid.Parse("d0a1b2c3-0000-4000-8000-000000000099");
    private static readonly Guid ShevchenkoStore = Guid.Parse("a7e1f000-0000-4000-9000-000000000004");
    private static readonly Guid BenderyStore = Guid.Parse("a7e1f000-0000-4000-9000-000000000008");

    private async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(string url)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(url);
        var text = await response.Content.ReadAsStringAsync();
        var body = text.Length > 0 ? JsonDocument.Parse(text).RootElement.Clone() : default;
        return (response.StatusCode, body);
    }

    private static string[] Names(JsonElement items) =>
        items.EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToArray();

    [Fact]
    public async Task Health_IsHealthy()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_ByPrefix_ReturnsContractShape()
    {
        var (status, body) = await GetAsync("/api/v1/drugs/search?q=ибупр");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("exact", body.GetProperty("mode").GetString());
        var first = body.GetProperty("items")[0];
        Assert.Equal("Ибупрофен 200 мг №50", first.GetProperty("name").GetString());
        Assert.Equal(1.0, first.GetProperty("score").GetDouble());
        Assert.Equal("BY", first.GetProperty("country").GetProperty("code").GetString());
        Assert.True(first.GetProperty("minPrice").GetDecimal() <= first.GetProperty("maxPrice").GetDecimal());
        Assert.True(first.GetProperty("pharmacyCount").GetInt32() > 0);
        Assert.EndsWith("Z", first.GetProperty("updatedAt").GetString());
        Assert.Equal("Таблетки", first.GetProperty("form").GetString());
        Assert.Equal("Ибупрофен", first.GetProperty("activeIngredient").GetString());
    }

    [Fact]
    public async Task Search_FindsByActiveIngredient()
    {
        var (_, body) = await GetAsync("/api/v1/drugs/search?q=дротаверин&sort=name");
        Assert.Equal(new[] { "Дротаверин 40 мг №20", "Но-шпа 40 мг №24" }, Names(body.GetProperty("items")));
    }

    [Fact]
    public async Task StaleOffers_AreHidden()
    {
        // у «Препарата без предложений» есть только цена, которую парсер не видел 30 дней
        var (_, drug) = await GetAsync($"/api/v1/drugs/{NoOffers}");
        Assert.Equal(0, drug.GetProperty("offers").GetArrayLength());
        Assert.Equal(0, drug.GetProperty("pharmacyCount").GetInt32());

        var (_, pharmacy) = await GetAsync("/api/v1/pharmacies/a7e1f000-0000-4000-9000-000000000001");
        Assert.DoesNotContain(pharmacy.GetProperty("offers").EnumerateArray(),
            o => o.GetProperty("drug").GetProperty("id").GetString() == NoOffers.ToString());
    }

    [Fact]
    public async Task Search_ToleratesTypos()
    {
        var (_, body) = await GetAsync("/api/v1/drugs/search?q=ибупорофен");
        Assert.Contains("Ибупрофен 200 мг №50", Names(body.GetProperty("items")));
    }

    [Fact]
    public async Task Search_MatchesManufacturer_AndBuildsFacetsBeforeFilters()
    {
        var (_, all) = await GetAsync("/api/v1/drugs/search?q=озон");
        Assert.Equal(3, all.GetProperty("total").GetInt32());

        var (_, filtered) = await GetAsync("/api/v1/drugs/search?q=ом&manufacturer=Озон");
        Assert.Equal(new[] { "Омепразол 20 мг №30" }, Names(filtered.GetProperty("items")));
        var manufacturers = filtered.GetProperty("facets").GetProperty("manufacturers").EnumerateArray()
            .Select(f => f.GetProperty("value").GetString()).ToArray();
        Assert.Contains("Dr. Reddy's", manufacturers); // фасеты считаются без учёта выбранного фильтра
    }

    [Fact]
    public async Task Search_SortsByPrice_WithNullsLast()
    {
        var (_, body) = await GetAsync("/api/v1/drugs/search?sort=price_asc&pageSize=50");
        var prices = body.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("minPrice").ValueKind == JsonValueKind.Null ? (decimal?)null : i.GetProperty("minPrice").GetDecimal())
            .ToList();
        Assert.Null(prices[^1]);
        var known = prices.Where(p => p is not null).ToList();
        Assert.Equal(known.OrderBy(p => p).ToList(), known);
    }

    [Fact]
    public async Task Search_Paginates()
    {
        var (_, page2) = await GetAsync("/api/v1/drugs/search?sort=name&page=2&pageSize=4");
        Assert.Equal(11, page2.GetProperty("total").GetInt32());
        Assert.Equal(4, page2.GetProperty("items").GetArrayLength());
        Assert.Equal(2, page2.GetProperty("page").GetInt32());
    }

    [Fact]
    public async Task SemanticSearch_UsesVectorScoresOrder()
    {
        factory.Semantic.SearchResults["изжога"] =
            [new SemanticHit(Omez, 0.81), new SemanticHit(Omeprazole, 0.9), new SemanticHit(Ibuprofen, 0.1)];

        var (status, body) = await GetAsync("/api/v1/drugs/search?q=изжога&mode=semantic");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("semantic", body.GetProperty("mode").GetString());
        // нерелевантное (ниже SemanticSearch:MinScore) отсекается
        Assert.Equal(new[] { "Омепразол 20 мг №30", "Омез 20 мг №30" }, Names(body.GetProperty("items")));
        Assert.Equal(2, body.GetProperty("total").GetInt32());
        Assert.Equal(0.9, body.GetProperty("items")[0].GetProperty("score").GetDouble());
    }

    [Fact]
    public async Task SemanticSearch_WhenServiceDown_Returns503Problem()
    {
        factory.Semantic.Available = false;
        try
        {
            var (status, body) = await GetAsync("/api/v1/drugs/search?q=головная боль&mode=semantic");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
            Assert.Equal(503, body.GetProperty("status").GetInt32());
        }
        finally
        {
            factory.Semantic.Available = true;
        }
    }

    [Fact]
    public async Task Suggest_PrefersPrefixMatches()
    {
        var (_, body) = await GetAsync("/api/v1/drugs/suggest?q=ом&limit=5");
        var names = Names(body.GetProperty("items"));
        Assert.Equal(2, names.Length);
        Assert.All(names, n => Assert.StartsWith("Ом", n));
    }

    [Fact]
    public async Task Popular_ExcludesDrugsWithoutOffers()
    {
        var (_, body) = await GetAsync("/api/v1/drugs/popular?limit=50");
        var counts = body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("pharmacyCount").GetInt32()).ToList();
        Assert.DoesNotContain("Препарат без предложений", Names(body.GetProperty("items")));
        Assert.Equal(counts.OrderByDescending(c => c).ToList(), counts); // в сиде у всех препаратов есть вещество
    }

    [Fact]
    public async Task Drug_ReturnsOffersSortedByPrice_WithNormalizedPharmacies()
    {
        factory.Semantic.Documents[Ibuprofen] = new SemanticDocument(Ibuprofen, "Обезболивающее.", "generated");

        var (status, body) = await GetAsync($"/api/v1/drugs/{Ibuprofen}");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Обезболивающее.", body.GetProperty("description").GetString());
        Assert.Equal("generated", body.GetProperty("descriptionSource").GetString());
        var offers = body.GetProperty("offers").EnumerateArray().ToList();
        Assert.Equal(body.GetProperty("pharmacyCount").GetInt32(), offers.Count);
        var prices = offers.Select(o => o.GetProperty("price").GetDecimal()).ToList();
        Assert.Equal(prices.OrderBy(p => p).ToList(), prices);
        Assert.Equal(body.GetProperty("minPrice").GetDecimal(), prices[0]);
        foreach (var offer in offers)
        {
            var phone = offer.GetProperty("pharmacy").GetProperty("phone").GetString()!;
            Assert.DoesNotContain("тел", phone);
        }
    }

    [Fact]
    public async Task Drug_WithoutOffers_HasNullPrices()
    {
        var (_, body) = await GetAsync($"/api/v1/drugs/{NoOffers}");
        Assert.Equal(JsonValueKind.Null, body.GetProperty("minPrice").ValueKind);
        Assert.Equal(0, body.GetProperty("offers").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("description").ValueKind);
    }

    [Fact]
    public async Task Drug_Unknown_Returns404()
    {
        var (status, _) = await GetAsync($"/api/v1/drugs/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, status);
    }

    [Fact]
    public async Task Analogs_ExcludeSelf_AndUnknownIds_KeepVectorOrder()
    {
        factory.Semantic.Similar[Nurofen] =
        [
            new SemanticHit(Nurofen, 1.0),
            new SemanticHit(Mig, 0.93),
            new SemanticHit(Guid.NewGuid(), 0.92), // есть в индексе, но удалён из БД
            new SemanticHit(Ibuprofen, 0.9),
            new SemanticHit(Omez, 0.2), // ниже SemanticSearch:MinSimilarity
        ];

        var (_, body) = await GetAsync($"/api/v1/drugs/{Nurofen}/analogs?limit=5");

        var items = body.GetProperty("items");
        Assert.Equal(new[] { "МИГ 400 мг №20", "Ибупрофен 200 мг №50" }, Names(items));
        Assert.Equal(0.93, items[0].GetProperty("similarity").GetDouble());
    }

    [Fact]
    public async Task Pharmacies_FilterByCity_AndExposeLocationWhenKnown()
    {
        var (_, body) = await GetAsync("/api/v1/pharmacies?city=Бендеры");
        var item = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal(BenderyStore.ToString(), item.GetProperty("id").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("location").ValueKind); // не геокодирована
        Assert.True(item.GetProperty("assortmentCount").GetInt32() > 0);
        Assert.Equal(3, body.GetProperty("cities").GetArrayLength());
        Assert.Equal(new[] { "ВиваФарм", "Доктор" }, body.GetProperty("networks").EnumerateArray().Select(n => n.GetString()).ToArray());
    }

    [Fact]
    public async Task Pharmacy_DoesNotDuplicateHouseNumber_AndListsOffers()
    {
        var (status, body) = await GetAsync($"/api/v1/pharmacies/{ShevchenkoStore}");
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("ул. Шевченко 81/7", body.GetProperty("address").GetString());
        Assert.Equal("8:00–20:00", body.GetProperty("hours").GetString());
        Assert.Equal("Таблетки", body.GetProperty("offers")[0].GetProperty("drug").GetProperty("form").GetString());
        Assert.Equal(46.8492, body.GetProperty("location").GetProperty("lat").GetDouble());
        var offer = body.GetProperty("offers")[0];
        Assert.True(offer.GetProperty("price").GetDecimal() > 0);
        Assert.False(string.IsNullOrEmpty(offer.GetProperty("drug").GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Stats_CountCatalog()
    {
        var (_, body) = await GetAsync("/api/v1/stats");
        Assert.Equal(11, body.GetProperty("drugs").GetInt32());
        Assert.Equal(8, body.GetProperty("pharmacies").GetInt32());
        Assert.Equal(2, body.GetProperty("networks").GetInt32());
        Assert.True(body.GetProperty("offers").GetInt32() > 0);
    }

    [Fact]
    public async Task LegacyODataEndpoint_Works()
    {
        var (status, body) = await GetAsync("/api/Drug?$top=2");
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(2, body.GetProperty("value").GetArrayLength());
    }
}
