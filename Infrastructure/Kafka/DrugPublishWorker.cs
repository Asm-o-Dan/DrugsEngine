using Application.Interfaces;
using Infrastructure.Configuration;
using Infrastructure.Dal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Kafka;

/// <summary>
/// Публикатор с опросом (transactional outbox по полю Drug.PublishedAt): периодически отправляет в Kafka препараты,
/// которые ещё не доставлены или изменились, и отмечает только подтверждённые брокером. Если Kafka недоступна,
/// ничего не теряется — повторим на следующем круге. Доставка «хотя бы один раз»: индексация в Qdrant идемпотентна.
/// </summary>
public sealed class DrugPublishWorker(
    IServiceScopeFactory scopeFactory,
    IKafkaProducer producer,
    IOptions<KafkaOptions> options,
    ILogger<DrugPublishWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Публикация в Kafka отключена (Kafka:Enabled = false)");
            return;
        }

        if (settings.PublishAllOnStartup)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
            var count = await db.Drugs.ExecuteUpdateAsync(s => s.SetProperty(d => d.PublishedAt, (DateTime?)null), stoppingToken);
            logger.LogInformation("Весь каталог ({Count}) поставлен в очередь на переиндексацию", count);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await PublishPendingAsync(settings.PublishBatchSize, stoppingToken);
                if (published > 0) continue; // есть ещё — сразу следующую пачку
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка публикации препаратов в Kafka");
            }
            await Task.Delay(settings.PublishInterval, stoppingToken);
        }
    }

    /// <returns>Сколько препаратов подтверждённо опубликовано.</returns>
    public async Task<int> PublishPendingAsync(int batchSize, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DrugsBotDbContext>();
        var batch = await db.Drugs.AsNoTracking()
            .Where(d => d.PublishedAt == null)
            .OrderBy(d => d.Id)
            .Take(batchSize)
            .ToListAsync(ct);
        if (batch.Count == 0) return 0;

        var delivered = (await producer.PublishAsync(batch, ct)).ToList();
        if (delivered.Count == 0) return 0;

        var now = DateTime.UtcNow;
        await db.Drugs.Where(d => delivered.Contains(d.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.PublishedAt, now), ct);
        logger.LogInformation("Опубликовано в Kafka для индексации: {Count}", delivered.Count);
        return delivered.Count;
    }
}
