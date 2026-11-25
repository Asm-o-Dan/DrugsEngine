using HtmlAgilityPack;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Infrastructure.Parsing;

/// <summary>Общая загрузка страниц для парсеров: пауза между запросами и ограниченное число повторов.</summary>
public class BaseParser(ILogger<BaseParser> logger, IOptions<ParsingOptions> options)
{
    private readonly RestClient _client = new(new RestClientOptions
    {
        Timeout = TimeSpan.FromSeconds(30),
        UserAgent = options.Value.UserAgent,
    });

    protected ParsingOptions Options => options.Value;

    /// <summary>Пауза перед каждым запросом; сеть может задать свою.</summary>
    protected virtual TimeSpan RequestDelay => options.Value.RequestDelay;

    /// <summary>Загружает страницу. Возвращает null, если все попытки неудачны.</summary>
    protected async Task<RestResponse?> FetchPageContent(string url, CancellationToken ct = default)
    {
        var settings = options.Value;
        for (var attempt = 1; attempt <= Math.Max(1, settings.MaxRetries); attempt++)
        {
            await Task.Delay(RequestDelay, ct);
            var response = await _client.ExecuteAsync(new RestRequest(url), ct);
            if (response.IsSuccessful)
                return response;

            var backoff = TimeSpan.FromSeconds(5 * attempt);
            logger.LogWarning("Не удалось загрузить {Url} (попытка {Attempt}/{Max}): {Status} {Error}. Повтор через {Backoff}",
                url, attempt, settings.MaxRetries, (int)response.StatusCode, response.ErrorMessage, backoff);
            if (attempt < settings.MaxRetries)
                await Task.Delay(backoff, ct);
        }

        logger.LogError("Страница {Url} недоступна после {Max} попыток", url, settings.MaxRetries);
        return null;
    }

    protected async Task<HtmlDocument?> FetchHtmlDocument(string url, CancellationToken ct = default)
    {
        var response = await FetchPageContent(url, ct);
        if (response?.Content is null) return null;
        var doc = new HtmlDocument();
        doc.LoadHtml(response.Content);
        return doc;
    }
}
