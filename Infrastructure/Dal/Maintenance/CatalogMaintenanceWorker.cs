using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Dal.Maintenance;

/// <summary>При старте объединяет дубли товаров (Catalog:MergeDuplicatesOnStartup).</summary>
public sealed class CatalogMaintenanceWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CatalogOptions> options,
    ILogger<CatalogMaintenanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.MergeDuplicatesOnStartup) return;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DrugMergeService>().RunAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось объединить дубли товаров");
        }
    }
}
