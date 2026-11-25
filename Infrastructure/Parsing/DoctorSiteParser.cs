using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Infrastructure.Parsing;

/// <summary>Строка прайса «Наличие в аптеках» сайта aptekadoktor.com.</summary>
public sealed record DoctorListingRow(
    string Name,
    string Manufacturer,
    string? CountryCode,
    string? CountryName,
    decimal Price,
    DateTime? UpdatedAtUtc,
    DoctorStoreInfo Store);

/// <summary>Аптека, как она указана в прайсе.</summary>
public sealed record DoctorStoreInfo(
    string Key,
    string City,
    string Street,
    string House,
    string Phone,
    double? Latitude,
    double? Longitude);

/// <summary>Аптека со страницы контактов (там есть часы работы).</summary>
public sealed record DoctorContactInfo(string? PhoneKey, string AddressKey, string? Hours);

/// <summary>
/// Разбор HTML сайта сети «Доктор» без сетевых запросов (чистые функции — покрыты тестами).
/// Формат строки аптеки в прайсе: «г.Тирасполь, ул.Ленина 15 Б, тел.: 0 (533) 6-66-00»,
/// координаты — в параметре ll= ссылки на карту.
/// </summary>
public static partial class DoctorSiteParser
{
    private static readonly TimeZoneInfo SiteTimeZone = FindTimeZone();

    public static IReadOnlyList<DoctorListingRow> ParseAvailability(string html, Action<string>? onSkipped = null)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var rows = doc.DocumentNode.SelectNodes("//table//tr");
        if (rows is null) return [];

        var result = new List<DoctorListingRow>();
        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells is null || cells.Count < 5) continue;
            if (cells[0].InnerText.Contains("Наименование")) continue; // заголовок

            var name = Clean(cells[0].InnerText);
            var (manufacturer, countryCode, countryName) = SplitManufacturer(Clean(cells[1].InnerText));
            var price = ParsePrice(cells[3].InnerText);
            var store = ParseStore(cells[4]);
            if (name.Length == 0 || manufacturer.Length == 0 || price is null || store is null)
            {
                onSkipped?.Invoke(Clean(row.InnerText));
                continue;
            }
            result.Add(new DoctorListingRow(name, manufacturer, countryCode, countryName, price.Value, ParseDate(cells[2].InnerText), store));
        }
        return result;
    }

    public static IReadOnlyList<DoctorContactInfo> ParseContacts(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var spans = doc.DocumentNode.SelectNodes("//span[contains(text(),'тел')]");
        if (spans is null) return [];

        return spans.Select(span =>
        {
            var text = Clean(span.InnerText);
            var phone = ContactPhone().Match(text);
            var hours = Hours().Match(text);
            var tel = PhoneWord().Match(text); // «тел» как отдельное слово, а не часть «Строителей»
            var telIndex = tel.Success ? tel.Index : -1;
            // «Аптека, ул. Ленина, 3 "а" , тел. …» → адрес между типом точки и телефоном
            var address = telIndex > 0 ? text[..telIndex] : text;
            var firstComma = address.IndexOf(',');
            if (firstComma >= 0) address = address[(firstComma + 1)..];
            return new DoctorContactInfo(
                phone.Success ? Domain.Entities.DrugStore.PhoneKey(phone.Groups[1].Value) : null,
                AddressKey(address),
                hours.Success ? $"{hours.Groups[1].Value}–{hours.Groups[2].Value}" : null);
        }).ToList();
    }

    /// <summary>Ключ для сопоставления адресов с разных страниц: название улицы + номер дома.</summary>
    public static string AddressKey(string address)
    {
        var text = address.ToLowerInvariant().Replace('ё', 'е');
        var words = StreetWords().Matches(text).Select(m => m.Value)
            .Where(w => w.Length >= 4 && w is not ("улица" or "бульвар" or "проспект" or "днестровск" or "тирасполь" or "бендеры"))
            .ToList();
        var house = HouseDigits().Match(text);
        return $"{words.LastOrDefault() ?? ""}|{(house.Success ? house.Value : "")}";
    }

    private static DoctorStoreInfo? ParseStore(HtmlNode cell)
    {
        var label = Clean(cell.InnerText);
        var m = StoreLabel().Match(label);
        if (!m.Success) return null;

        var key = Domain.Entities.DrugStore.PhoneKey(m.Groups["phone"].Value);
        if (key is null) return null;

        var (street, house) = SplitAddress(m.Groups["addr"].Value);
        var phone = LeadingTrunkZero().Replace(m.Groups["phone"].Value.Trim(), "");

        double? lat = null, lng = null;
        var src = WebUtility.HtmlDecode(cell.SelectSingleNode(".//a")?.GetAttributeValue("data-src", "") ?? "");
        var ll = MapCenter().Match(src);
        if (ll.Success
            && double.TryParse(ll.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var la)
            && double.TryParse(ll.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lo)
            && la is >= -90 and <= 90 && lo is >= -180 and <= 180)
        {
            lat = la;
            lng = lo;
        }

        return new DoctorStoreInfo(key, m.Groups["city"].Value.Trim(), street, house, phone, lat, lng);
    }

    /// <summary>См. <see cref="SourceText.SplitAddress"/>.</summary>
    public static (string Street, string House) SplitAddress(string address) => SourceText.SplitAddress(address);

    public const string UnknownManufacturer = SourceText.UnknownManufacturer;

    /// <summary>См. <see cref="SourceText.SplitManufacturer"/>.</summary>
    public static (string Manufacturer, string? CountryCode, string? CountryName) SplitManufacturer(string text) =>
        SourceText.SplitManufacturer(text);

    private static decimal? ParsePrice(string text)
    {
        var m = PriceNumber().Match(text.Replace(',', '.').Replace(" ", ""));
        return m.Success && decimal.TryParse(m.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) && price > 0
            ? price
            : null;
    }

    private static DateTime? ParseDate(string text)
    {
        if (!DateTime.TryParseExact(Clean(text), "dd.MM.yy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            return null;
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), SiteTimeZone);
    }

    private static string Clean(string text) =>
        Whitespace().Replace(WebUtility.HtmlDecode(text).Replace(' ', ' '), " ").Trim();

    private static TimeZoneInfo FindTimeZone()
    {
        // Время на сайте — местное (Приднестровье живёт по времени Кишинёва).
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Chisinau"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("UTC+2", TimeSpan.FromHours(2), "UTC+2", "UTC+2"); }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^\s*г\.\s*(?<city>[^,]+?)\s*,\s*(?<addr>.*?)\s*,\s*тел\.?\s*:?\s*(?<phone>[\d\s()\-]+?)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex StoreLabel();



    [GeneratedRegex(@"^0\s+")]
    private static partial Regex LeadingTrunkZero();

    [GeneratedRegex(@"[?&]ll=(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)")]
    private static partial Regex MapCenter();

    [GeneratedRegex(@"\d+(?:\.\d+)?")]
    private static partial Regex PriceNumber();

    [GeneratedRegex(@"тел\.?\s*:?\s*(\(?\d[\d\s()\-]*\d)", RegexOptions.IgnoreCase)]
    private static partial Regex ContactPhone();

    [GeneratedRegex(@"рабочие\s+часы\s*:?\s*(\d{1,2}:\d{2})\s*[-–]\s*(\d{1,2}:\d{2})", RegexOptions.IgnoreCase)]
    private static partial Regex Hours();

    [GeneratedRegex(@"(?<![А-Яа-яЁё])тел(?![А-Яа-яЁё]{2})", RegexOptions.IgnoreCase)]
    private static partial Regex PhoneWord();

    [GeneratedRegex(@"[а-яa-z]+")]
    private static partial Regex StreetWords();

    [GeneratedRegex(@"\d+")]
    private static partial Regex HouseDigits();
}
