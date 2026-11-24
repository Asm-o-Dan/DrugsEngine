namespace Infrastructure.Configuration;

/// <summary>Настройки БД. Строка подключения — в ConnectionStrings:Default.</summary>
public sealed class DatabaseOptions
{
    public const string Section = "Database";
    public int CommandTimeout { get; set; } = 30;
    /// <summary>Применять миграции при старте приложения.</summary>
    public bool MigrateOnStartup { get; set; } = true;
}

public sealed class KafkaOptions
{
    public const string Section = "Kafka";
    public bool Enabled { get; set; } = true;
    public string BootstrapServers { get; set; } = "kafka:9092";
    public string Topic { get; set; } = "drugs";
    /// <summary>
    /// Однократно опубликовать весь каталог при старте — для первичной индексации векторного поиска
    /// (по умолчанию в Kafka уходят только новые препараты).
    /// </summary>
    public bool PublishAllOnStartup { get; set; }
}

public sealed class ParsingOptions
{
    public const string Section = "Parsing";
    public bool Enabled { get; set; } = true;
    /// <summary>Пауза перед первым запуском после старта приложения.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>Интервал между полными проходами парсеров.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(6);
    /// <summary>Пауза между запросами к сайту аптеки (вежливость к чужому сайту).</summary>
    public TimeSpan RequestDelay { get; set; } = TimeSpan.FromSeconds(3);
    public int MaxRetries { get; set; } = 3;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
}

public sealed class SemanticSearchOptions
{
    public const string Section = "SemanticSearch";
    /// <summary>Адрес Python-сервиса векторного поиска. Пусто — семантический поиск отключён.</summary>
    public string? BaseUrl { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    // Пороги подобраны для multilingual-e5-small на реальном каталоге (см. README): у e5 оценки
    // релевантного и нерелевантного лежат в узкой полосе 0.83–0.88, поэтому одного абсолютного порога мало.

    /// <summary>Минимальная близость для поиска по смыслу — ниже точно нерелевантно.</summary>
    public double MinScore { get; set; } = 0.8;

    /// <summary>
    /// Без совпадения слов запроса с названием/веществом результат остаётся, только если он отстаёт
    /// от лучшего не больше чем на это значение.
    /// </summary>
    public double RelativeWindow { get; set; } = 0.01;

    /// <summary>
    /// Минимальная близость векторных аналогов. Пока у препаратов нет описаний, векторы строятся по названию
    /// и группируются по форме выпуска («капс №20»), поэтому порог высокий.
    /// </summary>
    public double MinSimilarity { get; set; } = 0.95;
}

public sealed class GeocodingOptions
{
    public const string Section = "Geocoding";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
    /// <summary>Nominatim требует осмысленный User-Agent с контактом владельца сервиса.</summary>
    public string UserAgent { get; set; } = "";
    /// <summary>Минимальная пауза между запросами (политика Nominatim — не чаще 1 в секунду).</summary>
    public TimeSpan MinInterval { get; set; } = TimeSpan.FromMilliseconds(1100);
    /// <summary>Через сколько повторять геокодирование адреса, который не удалось найти.</summary>
    public TimeSpan RetryFailedAfter { get; set; } = TimeSpan.FromDays(7);
    /// <summary>Сколько аптек геокодировать за один проход.</summary>
    public int BatchSize { get; set; } = 50;
}

public sealed class CatalogOptions
{
    public const string Section = "Catalog";
    /// <summary>
    /// Предложение, которое парсер не видел дольше этого срока, считается снятым с продажи и не показывается.
    /// </summary>
    public TimeSpan OfferMaxAge { get; set; } = TimeSpan.FromDays(3);
}
