using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Dal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Kafka;

/// <summary>
/// Публикует в Kafka весь каталог (Kafka:PublishAllOnStartup = true), чтобы Python-сервис проиндексировал
/// уже собранные препараты: обычно сообщение уходит только при создании нового препарата.
/// Повторная публикация безопасна — точка в Qdrant перезаписывается по Id.
/// </summary>
public sealed class CatalogPublishWorker(
    IServiceScopeFactory scopeFactory,
    IKafkaProducer producer,
    IOptions<KafkaOptions> options,
    ILogger<CatalogPublishWorker> logger) : BackgroundService
{
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || !options.Value.PublishAllOnStartup) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
        var published = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await db.Drugs.AsNoTracking()
                .OrderBy(d => d.Id)
                .Skip(published)
                .Take(BatchSize)
                .ToListAsync(stoppingToken);
            if (batch.Count == 0) break;

            foreach (var drug in batch) producer.ProduceDrug(drug);
            (producer as KafkaProducer)?.Flush(TimeSpan.FromSeconds(30));
            published += batch.Count;
        }
        logger.LogInformation("Каталог опубликован в Kafka для индексации: {Count} препаратов", published);
    }
}
