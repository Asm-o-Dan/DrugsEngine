using Domain.Entities;

namespace Application.Interfaces;

/// <summary>Публикация препаратов в Kafka для векторной индексации.</summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Отправляет пачку препаратов и ждёт подтверждения брокера.
    /// </summary>
    /// <returns>Id препаратов, доставку которых брокер подтвердил.</returns>
    Task<IReadOnlyCollection<Guid>> PublishAsync(IReadOnlyCollection<Drug> drugs, CancellationToken ct);
}
