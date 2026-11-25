using System.Globalization;
using System.Text.RegularExpressions;

namespace Infrastructure.Parsing;

/// <summary>
/// Ключ товара для сопоставления одного и того же продукта в разных сетях:
/// «Нольпаза 20мг №30 таб. (Пантопразол)» / KRKA d.d. Novo mesto и
/// «Нольпаза таб 20мг №30 (Пантопразол)» / KRKA → одинаковый ключ.
///
/// Правило консервативное: ложное объединение покажет пользователю цену чужого товара, а пропущенное —
/// только две карточки вместо одной. Поэтому должны совпасть ВСЕ значимые слова, все дозировки/объёмы,
/// фасовка, форма выпуска и производитель; без производителя или без чисел ключ не строится.
/// </summary>
public static partial class ProductKey
{
    // Аптечные сокращения и служебные слова, которые сети пишут по-разному и которые не отличают товар.
    private static readonly HashSet<string> Filler = new(StringComparer.Ordinal)
    {
        "таб", "табл", "таблетки", "таблетка", "капс", "капсулы", "капсула", "п", "о", "по", "пл", "плен", "пленоч",
        "пленочной", "оболоч", "оболочкой", "покрыт", "покрытые", "кишечнораств", "кишечнорастворимые", "гастрорезист",
        "д", "для", "приг", "приготовления", "р", "ра", "р-р", "р-ра", "раствор", "раствора", "ин", "инъекц", "инъекций",
        "инфуз", "инфузий", "фл", "флакон", "амп", "ампулы", "пор", "порошок", "сусп", "суспензия", "суспензии",
        "гран", "гранулы", "наружн", "наруж", "примен", "применения", "внутрь", "приема", "для", "и", "с", "в", "на",
        "шт", "уп", "упак", "пак", "саше", "мг", "мкг", "мл", "г", "гр", "л", "ме", "ед", "доз", "доза",
        "супп", "суппоз", "свечи", "рект", "ваг", "капли", "спрей", "мазь", "крем", "гель", "сироп", "драже",
        "пастилки", "пластырь", "аэрозоль", "лиоф", "лиофилизат", "эмульсия", "настойка", "линимент",
    };

    private static readonly Dictionary<string, string> Units = new(StringComparer.Ordinal)
    {
        ["мг"] = "mg", ["mg"] = "mg", ["мкг"] = "mcg", ["mcg"] = "mcg", ["г"] = "g", ["гр"] = "g", ["g"] = "g",
        ["мл"] = "ml", ["ml"] = "ml", ["л"] = "l", ["l"] = "l", ["%"] = "%", ["ме"] = "iu", ["ед"] = "iu", ["iu"] = "iu",
    };

    /// <returns>Ключ или null, если данных для надёжного сопоставления недостаточно.</returns>
    public static string? Compute(string name, string manufacturer, string? form)
    {
        var manufacturerKey = ManufacturerKey(manufacturer);
        if (manufacturerKey is null) return null;

        name = name.Trim().TrimEnd('*', ' ');
        // МНН в скобках в конце одна сеть пишет, другая нет — для ключа его не учитываем.
        // Остальные скобки различают варианты («(зелёный)», «(лимон и мёд)», «(42/102)») — их содержимое остаётся.
        if (ProductNameAnalyzer.DetectActiveIngredient(name) is not null && name.EndsWith(')'))
            name = name[..LastTopLevelParenthesis(name)];
        var text = name.ToLowerInvariant().Replace('ё', 'е').Replace(',', '.');

        // числа с единицами: «20мг», «800 гр.», «1.5л», «0.005%», «500мг/5мл»
        var quantities = Quantity().Matches(text)
            .Select(m => $"{Number(m.Groups["n"].Value)}{Units[m.Groups["u"].Value]}")
            .Distinct().Order(StringComparer.Ordinal).ToList();

        var pack = Pack().Match(text) is { Success: true } p ? $"n{int.Parse(p.Groups[1].Value, CultureInfo.InvariantCulture)}" : "";

        // слова — без дозировок/фасовки (они уже учтены). Отдельные числа остаются: «NAN 1» ≠ «NAN 2», «МИГ 400».
        var rest = Pack().Replace(Quantity().Replace(text, " "), " ").Replace('(', ' ').Replace(')', ' ');
        var words = Word().Matches(rest)
            .Select(m => m.Value)
            .Where(w => !Filler.Contains(w))
            .Distinct().Order(StringComparer.Ordinal).ToList();

        if (!words.Any(w => !char.IsDigit(w[0])) || (quantities.Count == 0 && pack.Length == 0)) return null;
        return string.Join('|', form ?? "", string.Join(' ', words), string.Join(' ', quantities), pack, manufacturerKey);
    }

    /// <summary>«KRKA d.d. Novo mesto» → «krka», «Nestle Suisse S.A.» / «NESTLE» → «nestle».</summary>
    public static string? ManufacturerKey(string manufacturer)
    {
        if (string.IsNullOrWhiteSpace(manufacturer) || manufacturer == SourceText.UnknownManufacturer) return null;
        var word = Word().Matches(manufacturer.ToLowerInvariant().Replace('ё', 'е'))
            .Select(m => m.Value)
            .FirstOrDefault(w => w.Length >= 3 && !char.IsDigit(w[0]) && w is not ("ооо" or "оао" or "зао" or "пао" or "ао"));
        return word;
    }

    private static int LastTopLevelParenthesis(string name)
    {
        var depth = 0;
        for (var i = name.Length - 1; i >= 0; i--)
        {
            if (name[i] == ')') depth++;
            else if (name[i] == '(' && --depth == 0) return i;
        }
        return name.Length;
    }

    private static string Number(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
            ? d.ToString("0.#####", CultureInfo.InvariantCulture)
            : value;

    [GeneratedRegex(@"(?<n>\d+(?:\.\d+)?)\s*(?<u>мкг|мг|mcg|mg|мл|ml|гр|г|g|л|l|%|ме|ед|iu)(?![а-яa-z])")]
    private static partial Regex Quantity();

    [GeneratedRegex(@"№\s*(\d+)")]
    private static partial Regex Pack();

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:-[\p{L}\p{N}]+)*")]
    private static partial Regex Word();

}
