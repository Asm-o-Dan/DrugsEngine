using Application.Interfaces;
using Application.UseCases.Commands.DrugItemCommands;
using Domain.Entities;
using MediatR;

namespace Infrastructure.Parsing;

public class ParsingManager
{
    private readonly List<IPharmacyParser> _parsers;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ParsingManager> _logger;

    public ParsingManager(IEnumerable<IPharmacyParser> parsers, IServiceScopeFactory scopeFactory,
        ILogger<ParsingManager> logger)
    {
        _parsers = parsers?.ToList() ?? throw new ArgumentNullException(nameof(parsers));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Запускает процесс парсинга всех аптек.
    /// </summary>
    public async Task ProcessAllPharmaciesAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Операция парсинга была отменена до начала выполнения.");
            return;
        }

        _logger.LogInformation("Запуск процесса парсинга аптек...");

        try
        {
            foreach (var parser in _parsers)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Операция парсинга была отменена во время обработки парсеров.");
                    break;
                }

                try
                {
                    _logger.LogInformation("Начинаем парсинг с использованием парсера: {ParserName}",
                        parser.GetType().Name);
                    await ProcessParserAsync(parser, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Критическая ошибка при работе парсера {ParserName}", parser.GetType().Name);
                    // Продолжаем работу с другими парсерами
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критическая ошибка в процессе парсинга всех аптек");
            throw; // Пробрасываем исключение выше, так как это фатальная ошибка для всего процесса
        }
        finally
        {
            _logger.LogInformation("Процесс парсинга завершен.");
        }
    }

    private async Task ProcessParserAsync(IPharmacyParser parser, CancellationToken cancellationToken)
    {
        try
        {
            var drugUrls = await parser.ParseDrugsLinksAsync();
            _logger.LogInformation("Получено {Count} URL для парсинга от парсера {ParserName}", drugUrls.Count,
                parser.GetType().Name);

            foreach (var url in drugUrls)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Операция парсинга была отменена во время обработки URL.");
                    return;
                }

                _logger.LogInformation("Начинаем парсинг URL: {Url}", url);

                try
                {
                    var drugItems = await parser.ParseAsync(url, cancellationToken);
                    if (drugItems != null && drugItems.Any())
                    {
                        _logger.LogInformation("Успешно получено {Count} DrugItems с URL: {Url}", drugItems.Count, url);
                        await UpdateDatabaseAsync(drugItems, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("Не найдено данных для парсинга на URL: {Url}", url);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Парсинг URL {Url} был отменен", url);
                    throw; // Пробрасываем исключение отмены выше
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при парсинге URL: {Url}", url);
                    // Продолжаем обработку следующих URL
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке парсера {ParserName}", parser.GetType().Name);
            throw; // Пробрасываем исключение выше для обработки в ProcessAllPharmaciesAsync
        }
    }

    /// <summary>
    /// Обновляет или добавляет цены в базе. Каждая строка — в своей области DI (свой DbContext):
    /// ошибка одной строки не оставляет «грязный» контекст для остальных.
    /// </summary>
    private async Task UpdateDatabaseAsync(List<DrugItem> drugItems, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Начало обновления базы данных для {Count} DrugItems", drugItems.Count);
        var failed = 0;

        foreach (var drugItem in drugItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var scope = _scopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            try
            {
                await mediator.Send(new CreateOrUpdateDrugItemCommand(drugItem), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (failed++ < 5)
                    _logger.LogError(ex, "Ошибка при сохранении цены {DrugName}", drugItem.Drug?.Name);
            }
        }

        _logger.LogInformation("Обновление базы завершено: {Ok} сохранено, {Failed} с ошибками",
            drugItems.Count - failed, failed);
    }
}