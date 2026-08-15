using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Entities;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Infrastructure.Kafka;

public class KafkaProducer : IKafkaProducer
{
    
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(ILogger<KafkaProducer> logger)
    {
        _logger = logger;
    }

    private const string Topic = "drugs";

    private static readonly ProducerConfig Config = new ProducerConfig {
        BootstrapServers = "kafka:9092", // Только localhost!
        SecurityProtocol = SecurityProtocol.Plaintext,
        MessageTimeoutMs = 10000, // Увеличенный таймаут
        Acks = Acks.All,
        EnableIdempotence = true
    };

    public void ProduceDrug(Drug drug)
    {
        var options = new JsonSerializerOptions {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 16
        };
        using var producer = new ProducerBuilder<Null, string>(Config)
            .SetErrorHandler((_, e) => _logger.LogError($"Ошибка Kafka: {e.Reason}"))
            .Build();

        try
        {
            var message = new Message<Null, string> 
            { 
                Value = JsonSerializer.Serialize(drug,options) 
            };

            producer.Produce(Topic, message, deliveryReport =>
            {
                if (deliveryReport.Error.IsError)
                    _logger.LogError($"Ошибка доставки: {deliveryReport.Error.Reason}");
                else
                    _logger.LogInformation($"Доставлено в партицию {deliveryReport.Partition}");
            });

            producer.Flush(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Критическая ошибка: {ex.Message}");
        }
    }
}