using Application.Interfaces.Repositories.BaseRepositories;
using Domain.Entities;

namespace Application.Interfaces.Repositories.DrugStoreRepositories;

public interface IDrugStoreReadRepository : IReadRepository<DrugStore>
{
    public Task<DrugStore?> GetByNumberAndNetworkAsync(int number, string network, CancellationToken cancellationToken);

    /// <summary>Аптека сети по устойчивому ключу источника (<see cref="DrugStore.ExternalKey"/>).</summary>
    public Task<DrugStore?> GetByExternalKeyAsync(string network, string externalKey, CancellationToken cancellationToken);

    /// <summary>Наибольший номер аптеки в сети (0, если аптек нет).</summary>
    public Task<int> GetMaxNumberAsync(string network, CancellationToken cancellationToken);
}