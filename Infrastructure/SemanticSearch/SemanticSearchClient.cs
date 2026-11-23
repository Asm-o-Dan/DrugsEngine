using System.Net;
using System.Net.Http.Json;
using Application.Interfaces;

namespace Infrastructure.SemanticSearch;

/// <summary>
/// HTTP-клиент DrugsEnginePythonService:
///   POST /v1/search            { query, limit }  → { items: [{ id, score }] }
///   GET  /v1/drugs/{id}/similar?limit=N          → { items: [{ id, score }] } | 404
///   GET  /v1/drugs/{id}                          → { id, description, descriptionSource } | 404
/// </summary>
public sealed class SemanticSearchClient(HttpClient http, ILogger<SemanticSearchClient> logger) : ISemanticSearchClient
{
    public bool IsConfigured => http.BaseAddress is not null;

    public async Task<IReadOnlyList<SemanticHit>> SearchAsync(string text, int limit, CancellationToken ct)
    {
        EnsureConfigured();
        var response = await SendAsync(() => http.PostAsJsonAsync("v1/search", new { query = text, limit }, ct), ct);
        return await ReadHitsAsync(response, ct);
    }

    public async Task<IReadOnlyList<SemanticHit>> SimilarAsync(Guid drugId, int limit, CancellationToken ct)
    {
        EnsureConfigured();
        var response = await SendAsync(() => http.GetAsync($"v1/drugs/{drugId}/similar?limit={limit}", ct), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return []; // препарат ещё не проиндексирован
        return await ReadHitsAsync(response, ct);
    }

    public async Task<SemanticDocument?> GetDocumentAsync(Guid drugId, CancellationToken ct)
    {
        if (!IsConfigured) return null;
        try
        {
            var response = await http.GetAsync($"v1/drugs/{drugId}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<DocumentResponse>(ct);
            return body is null ? null : new SemanticDocument(drugId, body.Description, body.DescriptionSource);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            // Описание — необязательная часть карточки: без него страница всё равно открывается.
            logger.LogWarning("Не удалось получить описание препарата {DrugId}: {Message}", drugId, ex.Message);
            return null;
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new SemanticSearchUnavailableException("Семантический поиск не настроен (SemanticSearch:BaseUrl).");
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            var response = await send();
            if (response.StatusCode != HttpStatusCode.NotFound && !response.IsSuccessStatusCode)
                throw new SemanticSearchUnavailableException($"Сервис векторного поиска ответил {(int)response.StatusCode}.");
            return response;
        }
        catch (HttpRequestException ex)
        {
            throw new SemanticSearchUnavailableException("Сервис векторного поиска недоступен.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new SemanticSearchUnavailableException("Сервис векторного поиска не ответил вовремя.", ex);
        }
    }

    private static async Task<IReadOnlyList<SemanticHit>> ReadHitsAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadFromJsonAsync<HitsResponse>(ct);
        return body?.Items
            .Where(i => Guid.TryParse(i.Id, out _))
            .Select(i => new SemanticHit(Guid.Parse(i.Id), i.Score))
            .ToList() ?? [];
    }

    private sealed record HitsResponse(List<HitItem> Items);
    private sealed record HitItem(string Id, double Score);
    private sealed record DocumentResponse(string? Description, string? DescriptionSource);
}
