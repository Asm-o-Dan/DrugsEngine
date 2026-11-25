using Application.Interfaces.Repositories.BaseRepositories;
using Domain.Entities;

namespace Application.Interfaces.Repositories.DrugRepositories;

public interface IDrugReadRepository: IReadRepository<Drug>
{
    Task<Drug?> GetByNameAsync(string drugName, CancellationToken cancellationToken);

    /// <summary>Препарат определяется парой «название + производитель».</summary>
    Task<Drug?> GetByNameAndManufacturerAsync(string drugName, string manufacturer, CancellationToken cancellationToken);

    /// <summary>Тот же товар, записанный другой сетью по-другому (см. Drug.MatchKey).</summary>
    Task<Drug?> GetByMatchKeyAsync(string matchKey, CancellationToken cancellationToken);
}