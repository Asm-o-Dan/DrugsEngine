using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Domain.Entities;
using HtmlAgilityPack;

namespace Infrastructure.Parsing;

/// <summary>Аптека сети «Столетник», как она указана на сайте.</summary>
public sealed record StoletnikStoreInfo(
    string Key,
    string Kind,
    string District,
    string City,
    string Street,
    string House,
    string Phone);

public sealed record StoletnikOffer(StoletnikStoreInfo Store, decimal Price, StockLevel? Level);

public sealed record StoletnikProduct(
    string Name,
    string Manufacturer,
    string? CountryCode,
    string? CountryName,
    IReadOnlyList<StoletnikOffer> Offers);

/// <summary>Аптека со страницы контактов: там есть часы работы.</summary>
public sealed record StoletnikContact(string PhoneKey, string? Hours);

/// <summary>
/// Разбор HTML сайта 100letnik.md (Webasyst Shop-Script) без сетевых запросов.
/// Товары — из sitemap-shop.xml; в карточке товара блок «Наличие в аптеках / фарм. пунктах»
/// (div.stock-item) со своей ценой и уровнем остатка по каждой точке; часы работы — на /kontakty/.
/// </summary>
public static partial class StoletnikSiteParser
{
    private static readonly HashSet<string> ServicePages = new(StringComparer.OrdinalIgnoreCase)
    {
        "o-kompanii", "dostavka-i-oplata", "kontakty", "category", "search", "order", "cart", "checkout", "my",
    };

    /// <summary>Адреса карточек товаров из sitemap-shop.xml (без категорий и служебных страниц).</summary>
    public static IReadOnlyList<string> ParseSitemap(string xml) =>
        SitemapLoc().Matches(xml)
            .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value).Trim())
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                          && uri.AbsolutePath.Trim('/') is { Length: > 0 } slug
                          && !slug.Contains('/')
                          && !ServicePages.Contains(slug))
            .Distinct()
            .ToList();

    /// <summary>Карточка товара; null — это не карточка (нет названия).</summary>
    public static StoletnikProduct? ParseProduct(string html, Action<string>? onSkipped = null)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var name = SourceText.Clean(doc.DocumentNode.SelectSingleNode("//h1")?.InnerText ?? "");
        if (name.Length == 0) return null;

        var manufacturerCell = doc.DocumentNode.SelectSingleNode(
            "//tr[contains(@class,'s-features-wrapper__feature')][.//*[normalize-space(text())='Производитель']]" +
            "/td[contains(@class,'s-features-wrapper__value')]");
        var (manufacturer, countryCode, countryName) =
            SourceText.SplitManufacturer(SourceText.Clean(manufacturerCell?.InnerText ?? ""));

        var offers = new List<StoletnikOffer>();
        var items = doc.DocumentNode.SelectNodes("//div[contains(concat(' ', normalize-space(@class), ' '), ' stock-item ')]");
        foreach (var item in items ?? Enumerable.Empty<HtmlNode>())
        {
            var offer = ParseStockItem(item);
            if (offer is null) onSkipped?.Invoke(SourceText.Clean(item.InnerText));
            else offers.Add(offer);
        }

        return new StoletnikProduct(name, manufacturer, countryCode, countryName, offers);
    }

    public static IReadOnlyList<StoletnikContact> ParseContacts(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var rows = doc.DocumentNode.SelectNodes("//div[contains(@class,'store-row')]");
        if (rows is null) return [];

        var result = new List<StoletnikContact>();
        foreach (var row in rows)
        {
            var phone = LabeledValue(row, "Телефон:", "store-label", "store-value");
            var key = DrugStore.PhoneKey(phone);
            if (key is null) continue;
            var hoursNode = row.SelectSingleNode(
                ".//div[contains(@class,'store-label')][normalize-space(text())='Время работы:']/following-sibling::div[1]");
            result.Add(new StoletnikContact(key, hoursNode is null ? null : NormalizeHours(hoursNode.InnerHtml)));
        }
        return result;
    }

    /// <summary>«Пн-Сб: 08:00 - 20:00&lt;br&gt;Вс: выходной» → «Пн–Сб 8:00–20:00, Вс выходной».</summary>
    public static string? NormalizeHours(string html)
    {
        var parts = LineBreak().Split(html)
            .Select(SourceText.Clean)
            .Where(p => p.Length > 0)
            .Select(p =>
            {
                var m = HoursPart().Match(p);
                if (!m.Success) return p;
                var days = m.Groups["days"].Value.Replace('-', '–').Replace(" ", "");
                var time = m.Groups["from"].Success
                    ? $"{TrimHour(m.Groups["from"].Value)}–{TrimHour(m.Groups["to"].Value)}"
                    : m.Groups["text"].Value.Trim().ToLowerInvariant();
                return $"{days} {time}";
            })
            .ToList();
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static string TrimHour(string time) => time.StartsWith('0') && time.Length == 5 ? time[1..] : time;

    private static StoletnikOffer? ParseStockItem(HtmlNode item)
    {
        var titles = item.SelectNodes(".//div[contains(@class,'imgddd')]//div[contains(@class,'title')]/div");
        var district = SourceText.Clean(titles?.ElementAtOrDefault(0)?.InnerText ?? "");
        var kind = SourceText.Clean(titles?.ElementAtOrDefault(1)?.InnerText ?? "");

        var address = LabeledValue(item, "Адрес:", "text-uppercase", null);
        var phone = LabeledValue(item, "Телефон:", "text-uppercase", null);
        var key = DrugStore.PhoneKey(phone);
        // «-Интернет Аптека-» — без адреса и телефона: это не точка продаж
        if (string.IsNullOrWhiteSpace(address) || key is null) return null;

        var parsedAddress = ParseAddress(address, district);
        if (parsedAddress is null) return null;

        var text = SourceText.Clean(item.InnerText);
        var price = PriceText().Match(text);
        if (!price.Success || !decimal.TryParse(price.Groups[1].Value.Replace(',', '.'), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var value) || value <= 0)
            return null;

        var level = StockText().Match(text) is { Success: true } s
            ? s.Groups[1].Value.ToLowerInvariant() switch
            {
                "мало" => StockLevel.Low,
                "достаточно" => StockLevel.Medium,
                "много" => StockLevel.High,
                _ => (StockLevel?)null,
            }
            : null;

        var (city, street, house) = parsedAddress.Value;
        var store = new StoletnikStoreInfo(key, kind, district, city, street, house, phone!.Trim());
        return new StoletnikOffer(store, value, level);
    }

    /// <summary>
    /// «г. Тирасполь, ул. Шевченко, 15 (Микрохирургия глаза)» → («Тирасполь», «ул. Шевченко», «15»).
    /// Если населённый пункт не указан, берётся из района («Тирасполь - > Балка»).
    /// </summary>
    public static (string City, string Street, string House)? ParseAddress(string address, string district)
    {
        var text = SourceText.Clean(Parenthetical().Replace(address, " "));
        string city;
        var m = CityPrefix().Match(text);
        if (m.Success)
        {
            city = m.Groups["city"].Value.Trim();
            text = text[m.Length..];
        }
        else
        {
            city = district.Split("->")[0].Replace("-", "").Trim();
        }
        if (city.Length == 0 || text.Trim(' ', ',').Length == 0) return null;
        var (street, house) = SourceText.SplitAddress(text);
        return (city, street, house);
    }

    private static string? LabeledValue(HtmlNode scope, string label, string labelClass, string? valueClass)
    {
        var labelNode = scope.SelectSingleNode(
            $".//div[contains(@class,'{labelClass}')][normalize-space(text())='{label}']");
        var value = labelNode?.SelectSingleNode(valueClass is null
            ? "following-sibling::div[1]"
            : $"following-sibling::div[contains(@class,'{valueClass}')][1]");
        var result = value is null ? null : SourceText.Clean(value.InnerText);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    [GeneratedRegex(@"<loc>([^<]+)</loc>")]
    private static partial Regex SitemapLoc();

    [GeneratedRegex(@"<br\s*/?>|\|", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreak();

    [GeneratedRegex(@"^(?<days>[А-Яа-я]{2}(?:\s*-\s*[А-Яа-я]{2})?)\s*:\s*(?:(?<from>\d{1,2}:\d{2})\s*[-–]\s*(?<to>\d{1,2}:\d{2})|(?<text>.+))$")]
    private static partial Regex HoursPart();

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex Parenthetical();

    [GeneratedRegex(@"^\s*(?:г|с|пгт|п|пос)\.\s*(?<city>[^,]+?)\s*,\s*", RegexOptions.IgnoreCase)]
    private static partial Regex CityPrefix();

    [GeneratedRegex(@"Цена:\s*([\d]+(?:[.,]\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex PriceText();

    [GeneratedRegex(@"На складе:\s*([А-Яа-яЁё]+)", RegexOptions.IgnoreCase)]
    private static partial Regex StockText();
}
