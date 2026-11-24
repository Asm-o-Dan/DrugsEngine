using System.Text.RegularExpressions;

namespace Infrastructure.Parsing;

/// <summary>
/// Извлекает из названия товара в прайсе форму выпуска и действующее вещество.
/// Аптека пишет их в свободной форме: «Нольпаза таб 40 мг №30(Пантопразол)»,
/// «Глаупрост капли глазные р-р 0,005% 2,5мл(Латанопрост)». Правила консервативные:
/// лучше не распознать, чем приписать товару чужое вещество.
/// </summary>
public static partial class ProductNameAnalyzer
{
    private static readonly (Regex Pattern, string Form)[] Forms =
    [
        (Word("таб|табл|таблетки|таблетка"), "Таблетки"),
        (Word("капс|капсулы|капсула"), "Капсулы"),
        (Word("драже"), "Драже"),
        (Word("капли"), "Капли"),
        (Word("супп|суппоз|суппозитории|свечи"), "Суппозитории"),
        (Word("мазь"), "Мазь"),
        (Word("крем"), "Крем"),
        (Word("гель"), "Гель"),
        (Word("линимент"), "Линимент"),
        (Word("спрей"), "Спрей"),
        (Word("аэрозоль"), "Аэрозоль"),
        (Word("сироп"), "Сироп"),
        (Word("сусп|суспензия"), "Суспензия"),
        (Word("эмульсия"), "Эмульсия"),
        (Word("пор|порошок"), "Порошок"),
        (Word("гран|гранулы"), "Гранулы"),
        (Word("пастилки"), "Пастилки"),
        (Word("пластырь"), "Пластырь"),
        (Word("лиоф|лиофилизат"), "Лиофилизат"),
        (Word("настойка"), "Настойка"),
        (Word("р-р|раствор"), "Раствор"),
    ];

    private static readonly HashSet<string> MedicalForms =
    [
        "Таблетки", "Капсулы", "Драже", "Капли", "Суппозитории", "Мазь", "Крем", "Гель", "Линимент", "Спрей", "Аэрозоль",
        "Сироп", "Суспензия", "Эмульсия", "Порошок", "Гранулы", "Пастилки", "Лиофилизат", "Раствор",
    ];

    /// <summary>Форма выпуска или null. Определяется по самому раннему упоминанию в названии.</summary>
    public static string? DetectForm(string name)
    {
        var text = name.ToLowerInvariant().Replace('ё', 'е');
        (int Index, string Form)? best = null;
        foreach (var (pattern, form) in Forms)
        {
            var m = pattern.Match(text);
            if (!m.Success) continue;
            // «р-р» у бандажей и манжет — это размер, а не раствор: требуем дозировку или объём
            if (form == "Раствор" && !Dosage().IsMatch(text)) continue;
            if (best is null || m.Index < best.Value.Index) best = (m.Index, form);
        }
        return best?.Form;
    }

    /// <summary>
    /// Действующее вещество из скобок в конце названия — только у лекарственных форм
    /// и только если текст похож на МНН (с заглавной буквы, без размеров и артикулов).
    /// </summary>
    public static string? DetectActiveIngredient(string name)
    {
        var form = DetectForm(name);
        if (form is null || !MedicalForms.Contains(form)) return null;

        var trailing = TrailingParentheses(name.TrimEnd());
        if (trailing is null) return null;

        var candidate = Spaces().Replace(trailing, " ").Trim(' ', ',', '.', '(', ')');
        if (candidate.Length is < 3 or > 120) return null;
        if (!IsCyrillicOrLatin(candidate[0])) return null;
        if (NotAnIngredient().IsMatch(candidate)) return null;
        // обрывки сокращений вроде «син уп»: все слова не длиннее трёх букв
        if (candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(w => w.Length <= 3)) return null;

        candidate = Combination().Replace(candidate, " + "); // «Амлодипин\лизиноприл», «А+Б»
        candidate = Spaces().Replace(candidate, " ").Trim();
        return char.ToUpper(candidate[0]) + candidate[1..];
    }

    /// <summary>Содержимое последней пары скобок, если название ими заканчивается (с учётом вложенности).</summary>
    private static string? TrailingParentheses(string name)
    {
        if (!name.EndsWith(')')) return null;
        var depth = 0;
        for (var i = name.Length - 1; i >= 0; i--)
        {
            if (name[i] == ')') depth++;
            else if (name[i] == '(' && --depth == 0) return name[(i + 1)..^1];
        }
        return null;
    }

    private static bool IsCyrillicOrLatin(char c) => c is (>= 'А' and <= 'я') or 'Ё' or 'ё' or (>= 'A' and <= 'z');

    private static Regex Word(string alternatives) =>
        new($@"(?<![а-яa-z0-9])(?:{alternatives})\.?(?![а-яa-z])", RegexOptions.Compiled);

    [GeneratedRegex(@"\d+(?:[.,]\d+)?\s*(?:мг|мкг|мл|г/|%|ме|ед)", RegexOptions.IgnoreCase)]
    private static partial Regex Dosage();

    // размеры, артикулы, назначение («для …»), коды — не вещество
    [GeneratedRegex(@"\d+\s*(?:см|мм|шт|гр|г)\b|\bарт\b|арт\.|\bкод\b|\bдля\b|\bд/|размер|обхват|\bцвет|ср-во|средство|^\d|^бад$", RegexOptions.IgnoreCase)]
    private static partial Regex NotAnIngredient();

    [GeneratedRegex(@"\s*\+\s*|(?<=\p{L})\s*[\\/]\s*(?=\p{L})")]
    private static partial Regex Combination();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
