using System.Text.Json;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Entities;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Kafka;

/// <summary>
/// Публикует новые препараты в Kafka для векторизации (DrugsEnginePythonService).
/// Один продюсер на всё приложение: создавать его на каждое сообщение дорого.
/// </summary>
public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly KafkaOptions _options;
    private readonly Lazy<IProducer<string, string>> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        _options = options.Value;
        _producer = new Lazy<IProducer<string, string>>(() =>
            new ProducerBuilder<string, string>(new ProducerConfig
                {
                    BootstrapServers = _options.BootstrapServers,
                    Acks = Acks.All,
                    EnableIdempotence = true,
                    MessageTimeoutMs = 10_000,
                })
                .SetErrorHandler((_, e) => _logger.LogError("Ошибка Kafka: {Reason}", e.Reason))
                .Build());
    }

    public void ProduceDrug(Drug drug)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Kafka отключена, событие для {DrugId} не отправлено", drug.Id);
            return;
        }

        // Формат сообщения согласован с DrugsEnginePythonService (Drug.from_json): PascalCase, Id обязателен.
        var payload = JsonSerializer.Serialize(new DrugMessage(
            drug.Id, drug.Name, drug.Manufacturer, drug.CountryCodeId, drug.Form, drug.ActiveIngredient));
        _producer.Value.Produce(_options.Topic, new Message<string, string> { Key = drug.Id.ToString(), Value = payload }, report =>
        {
            if (report.Error.IsError)
                _logger.LogError("Не удалось доставить событие для {DrugId}: {Reason}", drug.Id, report.Error.Reason);
        });
    }

    /// <summary>Дождаться отправки накопленных сообщений.</summary>
    public void Flush(TimeSpan timeout)
    {
        if (_producer.IsValueCreated) _producer.Value.Flush(timeout);
    }

    public void Dispose()
    {
        if (!_producer.IsValueCreated) return;
        _producer.Value.Flush(TimeSpan.FromSeconds(10));
        _producer.Value.Dispose();
    }

    private sealed record DrugMessage(
        Guid Id, string Name, string Manufacturer, string? CountryCodeId, string? Form, string? ActiveIngredient);
}
