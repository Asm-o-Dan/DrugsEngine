using HtmlAgilityPack;
using RestSharp;

namespace Infrastructure.Parsing;

public class BaseParser(ILogger<BaseParser> logger)
{
    /// <summary>
    /// Загружает HTML-страницу.
    /// <pu/summary>
    protected async Task<RestResponse> FetchPageContent(string url)
    {
        await Task.Delay(3*1000);
        var client = new RestClient();
        var request = new RestRequest(url);
        request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            await Task.Delay(10 * 1000);
            return await FetchPageContent(url);
        }
        if (!response.IsSuccessful)
        {
            logger.LogError($"Ошибка загрузки {url}: Статуч ответа: {response.StatusCode}");
            return null;
        }
        return response;
    }

    /// <summary>
    /// Загружает HTML-страницу и возвращает <see cref="HtmlDocument"/>.
    /// </summary>
    protected async Task<HtmlDocument> FetchHtmlDocument(string url)
    {
        var response = await FetchPageContent(url);
        if (response == null) return null;
        var doc = new HtmlDocument();
        doc.LoadHtml(response.Content);
        return doc;
    }
}