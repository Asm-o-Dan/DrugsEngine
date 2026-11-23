using Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.API.Controllers.V1;

[ApiController]
[Route("api/v1/pharmacies")]
[Produces("application/json")]
public sealed class PharmaciesController(ICatalogQueries catalog) : ControllerBase
{
    [HttpGet]
    public Task<PharmacyListDto> List([FromQuery] string? network, [FromQuery] string? city, [FromQuery] string? q, CancellationToken ct) =>
        catalog.ListPharmaciesAsync(network, city, q, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PharmacyDetailsDto>> Get(Guid id, CancellationToken ct) =>
        await catalog.GetPharmacyAsync(id, ct) is { } pharmacy ? pharmacy : NotFound();
}

[ApiController]
[Route("api/v1/stats")]
[Produces("application/json")]
public sealed class StatsController(ICatalogQueries catalog) : ControllerBase
{
    [HttpGet]
    public Task<CatalogStatsDto> Get(CancellationToken ct) => catalog.GetStatsAsync(ct);
}
