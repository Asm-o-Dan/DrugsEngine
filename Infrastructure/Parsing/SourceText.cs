using System.Net;
using System.Text.RegularExpressions;

namespace Infrastructure.Parsing;

/// <summary>Общие правила разбора текста с сайтов аптечных сетей.</summary>
public static partial class SourceText
{
    public static string Clean(string text) =>
        Whitespace().Replace(WebUtility.HtmlDecode(text).Replace('\u00a0', ' '), " ").Trim();

    /// <summary>«ул.Ленина 15 Б» → («ул. Ленина», «15Б»); «ул.Калинина, 64» → («ул. Калинина», «64»).</summary>
    public static (string Street, string House) SplitAddress(string address)
    {
        var text = Clean(address.Replace(',', ' '));
        string street, house;
        if (NoNumber().Match(text) is { Success: true } noNumber)
        {
            // «ул. Садовая б/н» — дом без номера
            (street, house) = (noNumber.Groups["street"].Value, "б/н");
        }
        else if (HouseMarker().Match(text) is { Success: true } marked)
        {
            // «мкр. Консервный завод д.11 п.№3» — номер после «д.»
            (street, house) = (marked.Groups["street"].Value, marked.Groups["house"].Value.Trim());
        }
        else if (StreetAndHouse().Match(text) is { Success: true } m)
        {
            (street, house) = (m.Groups["street"].Value, Regex.Replace(m.Groups["house"].Value, @"\s+", "").ToUpperInvariant());
        }
        else
        {
            (street, house) = (text, "б/н");
        }
        street = Clean(AbbreviationDot().Replace(street, ". ")).TrimEnd(',', ' ');
        return (street, house);
    }

    public const string UnknownManufacturer = "Не указан";

    /// <summary>
    /// Колонка «Производитель» бывает в трёх видах: «Gedeon Richter», «Турция»,
    /// «Balkan Pharmaceuticals SRL Молдова», а также «Лаборатория Эксперт ; Россия» / «Orion, Финляндия».
    /// Страна, которой нет в справочнике (например, «ПМР»), остаётся неизвестной — не подставляем выдуманную.
    /// </summary>
    public static (string Manufacturer, string? CountryCode, string? CountryName) SplitManufacturer(string text)
    {
        text = Clean(text).Trim(' ', ',', ';');
        var separator = text.LastIndexOfAny([',', ';']);
        if (separator > 0)
        {
            var manufacturer = text[..separator].Trim(' ', ',', ';');
            var code = ResolveCountry(text[(separator + 1)..]);
            return (manufacturer.Length > 0 ? manufacturer : UnknownManufacturer, code, CountryName(code));
        }

        if (ResolveCountry(text) is { } onlyCountry)
            return (UnknownManufacturer, onlyCountry, CountryName(onlyCountry));

        // «Юнилевер Русь Россия» — страна последними словами
        var words = text.Split(' ');
        for (var take = Math.Min(3, words.Length - 1); take >= 1; take--)
        {
            if (ResolveCountry(string.Join(' ', words[^take..])) is { } code)
                return (string.Join(' ', words[..^take]), code, CountryName(code));
        }
        return (text, null, null);
    }

    private static string? ResolveCountry(string text)
    {
        var name = text.Trim(' ', '.', ',', ';');
        if (name.Length == 0) return null;
        // «Франция/Россия» — страна производства идёт первой
        var first = name.Split('/')[0].Trim();
        return Domain.Validators.CountryCodes.GetCodeByRussianName(first);
    }

    private static string? CountryName(string? code) =>
        code is null ? null : Domain.Validators.CountryCodes.GetRussianName(code) ?? code;


    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    // «15 Б», «50А», «81/7», «13-а/2»
    [GeneratedRegex(@"^(?<street>.*?\D)\s*(?<house>\d+(?:\s*-?\s*[А-Яа-яA-Za-z](?![А-Яа-яA-Za-z]))?(?:\s*/\s*\d+[А-Яа-яA-Za-z]?)?)\s*/?\s*$")]
    private static partial Regex StreetAndHouse();

    [GeneratedRegex(@"^(?<street>.+?)\s*б/н\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex NoNumber();

    [GeneratedRegex(@"^(?<street>.+?)\s+д\.\s*(?<house>\d.*)$")]
    private static partial Regex HouseMarker();

    [GeneratedRegex(@"\.(?=[А-ЯЁA-Z])")]
    private static partial Regex AbbreviationDot();
}
