using Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Api.Tests;

/// <summary>
/// Поднимает API на отдельной временной базе PostgreSQL (миграции + scripts/dev-seed.sql).
/// Сервер Postgres берётся из переменной TEST_POSTGRES (по умолчанию localhost, postgres/postgres).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string ServerConnection =
        Environment.GetEnvironmentVariable("TEST_POSTGRES") ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    private readonly string _database = $"drugsengine_test_{Guid.NewGuid():N}";

    public FakeSemanticSearch Semantic { get; } = new();
    public FakeKafkaProducer Kafka { get; } = new();

    private string DatabaseConnection => new NpgsqlConnectionStringBuilder(ServerConnection) { Database = _database }.ConnectionString;

    public async Task InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(ServerConnection))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_database}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        // Строку подключения Program читает до того, как применяются переопределения фабрики,
        // поэтому передаём её через переменные окружения.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", DatabaseConnection);
        Environment.SetEnvironmentVariable("Parsing__Enabled", "false");
        Environment.SetEnvironmentVariable("Kafka__Enabled", "false");
        // Фейковые оценки в тестах — «удобные» числа, а не оценки e5: пороги задаём явно.
        Environment.SetEnvironmentVariable("SemanticSearch__MinScore", "0.3");
        Environment.SetEnvironmentVariable("SemanticSearch__RelativeWindow", "0.2");
        Environment.SetEnvironmentVariable("SemanticSearch__MinSimilarity", "0.5");

        _ = Server; // старт приложения → миграции

        await using var db = new NpgsqlConnection(DatabaseConnection);
        await db.OpenAsync();
        var seed = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "dev-seed.sql"));
        await using var cmd = new NpgsqlCommand(seed, db);
        await cmd.ExecuteNonQueryAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ISemanticSearchClient>(Semantic);
            services.AddSingleton<IKafkaProducer>(Kafka);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var admin = new NpgsqlConnection(ServerConnection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}

/// <summary>Подменяет Python-сервис векторного поиска.</summary>
public sealed class FakeSemanticSearch : ISemanticSearchClient
{
    public bool Available { get; set; } = true;
    public Dictionary<string, List<SemanticHit>> SearchResults { get; } = new();
    public Dictionary<Guid, List<SemanticHit>> Similar { get; } = new();
    public Dictionary<Guid, SemanticDocument> Documents { get; } = new();

    public bool IsConfigured => true;

    public Task<IReadOnlyList<SemanticHit>> SearchAsync(string text, int limit, CancellationToken ct)
    {
        if (!Available) throw new SemanticSearchUnavailableException("Сервис векторного поиска недоступен.");
        return Task.FromResult<IReadOnlyList<SemanticHit>>(SearchResults.GetValueOrDefault(text, []).Take(limit).ToList());
    }

    public Task<IReadOnlyList<SemanticHit>> SimilarAsync(Guid drugId, int limit, CancellationToken ct)
    {
        if (!Available) throw new SemanticSearchUnavailableException("Сервис векторного поиска недоступен.");
        return Task.FromResult<IReadOnlyList<SemanticHit>>(Similar.GetValueOrDefault(drugId, []).Take(limit).ToList());
    }

    public Task<SemanticDocument?> GetDocumentAsync(Guid drugId, CancellationToken ct) =>
        Task.FromResult(Available ? Documents.GetValueOrDefault(drugId) : null);
}

/// <summary>Подменяет Kafka: «доставляет» только разрешённые сообщения.</summary>
public sealed class FakeKafkaProducer : IKafkaProducer
{
    public Func<Domain.Entities.Drug, bool> Deliver { get; set; } = _ => true;
    public List<Guid> Sent { get; } = new();

    public Task<IReadOnlyCollection<Guid>> PublishAsync(IReadOnlyCollection<Domain.Entities.Drug> drugs, CancellationToken ct)
    {
        lock (Sent) Sent.AddRange(drugs.Select(d => d.Id));
        return Task.FromResult<IReadOnlyCollection<Guid>>(drugs.Where(Deliver).Select(d => d.Id).ToList());
    }
}
