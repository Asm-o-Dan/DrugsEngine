using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Infrastructure.Telemetry;

public static class DrugsEngineTelemetry
{
    public const string ServiceName = "DrugsEngine";
    public const string ServiceVersion = "1.0.0";

    // Activity Sources for Distributed Tracing
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);
    public static readonly ActivitySource ParsingActivitySource = new("DrugsEngine.Parsing", ServiceVersion);
    public static readonly ActivitySource KafkaActivitySource = new("DrugsEngine.Kafka", ServiceVersion);

    // Metrics Meter
    public static readonly Meter Meter = new(ServiceName, ServiceVersion);
    public static readonly Meter ParsingMeter = new("DrugsEngine.Parsing", ServiceVersion);
    public static readonly Meter KafkaMeter = new("DrugsEngine.Kafka", ServiceVersion);

    // Custom Metrics
    public static readonly Counter<long> DrugsParsedCounter = ParsingMeter.CreateCounter<long>(
        "drugsengine_parsing_drugs_discovered_total",
        description: "Total number of drugs discovered during pharmacy parsing"
    );

    public static readonly Histogram<double> ParsingDuration = ParsingMeter.CreateHistogram<double>(
        "drugsengine_parsing_duration_seconds",
        unit: "s",
        description: "Duration of pharmacy parsing runs in seconds"
    );

    public static readonly Counter<long> ParsingErrorsCounter = ParsingMeter.CreateCounter<long>(
        "drugsengine_parsing_errors_total",
        description: "Total number of errors encountered during pharmacy parsing"
    );

    public static readonly Counter<long> KafkaProducedCounter = KafkaMeter.CreateCounter<long>(
        "drugsengine_kafka_messages_produced_total",
        description: "Total number of messages successfully published to Kafka"
    );

    public static readonly Counter<long> KafkaProduceErrorsCounter = KafkaMeter.CreateCounter<long>(
        "drugsengine_kafka_produce_errors_total",
        description: "Total number of Kafka message publishing errors"
    );
}
