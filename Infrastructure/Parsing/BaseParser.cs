using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace Infrastructure.Parsing;

public class BaseParser
{
    private readonly ILogger _logger;
    private readonly RestClient _client;

    public BaseParser(ILogger logger, RestClient? client = null)
    {
        _logger = logger;
        _client = client ?? new RestClient(new RestClientOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            MaxTimeout = 30000,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
    }

    /// <summary>
    /// Загружает HTML-страницу с ограниченным числом повторов и экспоненциальной задержкой.
    /// </summary>
    /// <param name="url">Целевой URL</param>
    /// <param name="maxRetries">Максимальное число попыток</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Экземпляр <see cref="RestResponse"/> или null в случае неудачи</returns>
    protected async Task<RestResponse?> FetchPageContent(string url, int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest(url);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _client.ExecuteAsync(request, cancellationToken);
                if (response.IsSuccessful)
                {
                    return response;
                }

                // 4xx ошибки (например 404 Not Found, 403 Forbidden, 410 Gone) не требуют повторов
                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    _logger.LogWarning("Клиентская ошибка при загрузке {Url}: HTTP {StatusCode}. Повтор отменён.", url, response.StatusCode);
                    return null;
                }

                _logger.LogWarning("Попытка {Attempt}/{MaxRetries} не удалась для {Url}: HTTP {StatusCode} ({ErrorMessage})",
                    attempt, maxRetries, url, response.StatusCode, response.ErrorMessage);

                if (attempt < maxRetries)
                {
                    var delaySeconds = Math.Pow(2, attempt);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Запрос к {Url} отменён по токену отмены.", url);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Исключение при попытке {Attempt}/{MaxRetries} загрузки {Url}", attempt, maxRetries, url);
                if (attempt < maxRetries)
                {
                    var delaySeconds = Math.Pow(2, attempt);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
            }
        }

        _logger.LogError("Исчерпаны все {MaxRetries} попыток загрузки {Url}", maxRetries, url);
        return null;
    }

    /// <summary>
    /// Загружает HTML-страницу и возвращает <see cref="HtmlDocument"/>.
    /// </summary>
    protected async Task<HtmlDocument?> FetchHtmlDocument(string url, CancellationToken cancellationToken = default)
    {
        var response = await FetchPageContent(url, cancellationToken: cancellationToken);
        if (response == null || string.IsNullOrWhiteSpace(response.Content))
        {
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(response.Content);
        return doc;
    }
}