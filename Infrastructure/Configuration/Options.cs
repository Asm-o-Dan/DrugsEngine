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
    /// <summary>Минимальная косинусная близость для результатов поиска по смыслу (отсекает нерелевантное).</summary>
    public double MinScore { get; set; } = 0.3;
    /// <summary>Минимальная близость для блока «Похожие препараты».</summary>
    public double MinSimilarity { get; set; } = 0.5;
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
