using Infrastructure.Configuration;
using Infrastructure.Geocoding;
using Microsoft.Extensions.Options;

namespace Infrastructure.Parsing;

/// <summary>
/// Периодический сбор цен с сайтов аптек и геокодирование новых аптек.
/// Работает в фоне и не блокирует запуск API.
/// </summary>
public sealed class ParsingWorker(
    ParsingManager parsingManager,
    IServiceScopeFactory scopeFactory,
    IOptions<ParsingOptions> options,
    ILogger<ParsingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Парсинг отключён (Parsing:Enabled = false)");
            return;
        }

        await Task.Delay(settings.InitialDelay, stoppingToken);
        using var timer = new PeriodicTimer(settings.Interval);
        do
        {
            try
            {
                await parsingManager.ProcessAllPharmaciesAsync(stoppingToken);
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<StoreGeocodingService>().GeocodePendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Ошибка одного прохода не должна останавливать сервис — повторим по расписанию.
                logger.LogError(ex, "Проход парсинга завершился ошибкой");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
