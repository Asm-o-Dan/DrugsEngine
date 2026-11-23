using Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.API.Controllers.V1;

/// <summary>Каталог препаратов для веб-клиента (контракт — DrugsEngine.Web/docs/API.md).</summary>
[ApiController]
[Route("api/v1/drugs")]
[Produces("application/json")]
public sealed class DrugsController(ICatalogQueries catalog) : ControllerBase
{
    private const int MaxPageSize = 50;

    /// <summary>Поиск: mode = exact | semantic; sort = relevance | price_asc | price_desc | name.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<SearchResponseDto>> Search(
        [FromQuery] string? q,
        [FromQuery] string? mode,
        [FromQuery] string? sort,
        [FromQuery] string? manufacturer,
        [FromQuery] string? country,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var query = new SearchQuery(
            Q: (q ?? "").Trim(),
            Mode: mode == "semantic" ? SearchMode.Semantic : SearchMode.Exact,
            Sort: sort switch
            {
                "price_asc" => SearchSort.PriceAsc,
                "price_desc" => SearchSort.PriceDesc,
                "name" => SearchSort.Name,
                _ => SearchSort.Relevance,
            },
            Manufacturer: string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
            Country: string.IsNullOrWhiteSpace(country) ? null : country,
            Page: Math.Max(1, page),
            PageSize: Math.Clamp(pageSize, 1, MaxPageSize));
        if (query.Q.Length > 200) return Problem(title: "Слишком длинный запрос", statusCode: 400);
        return await catalog.SearchAsync(query, ct);
    }

    [HttpGet("suggest")]
    public async Task<ItemsDto<SuggestionDto>> Suggest([FromQuery] string? q, [FromQuery] int limit = 7, CancellationToken ct = default) =>
        new(await catalog.SuggestAsync((q ?? "").Trim(), Math.Clamp(limit, 1, 20), ct));

    [HttpGet("popular")]
    public async Task<ItemsDto<DrugSummaryDto>> Popular([FromQuery] int limit = 8, CancellationToken ct = default) =>
        new(await catalog.GetPopularAsync(Math.Clamp(limit, 1, 50), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DrugDetailsDto>> Get(Guid id, CancellationToken ct) =>
        await catalog.GetDrugAsync(id, ct) is { } drug ? drug : NotFound();

    [HttpGet("{id:guid}/analogs")]
    public async Task<ItemsDto<AnalogDto>> Analogs(Guid id, [FromQuery] int limit = 6, CancellationToken ct = default) =>
        new(await catalog.GetAnalogsAsync(id, Math.Clamp(limit, 1, 20), ct));
}
