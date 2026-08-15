using Application.Interfaces.Repositories.BaseRepositories;
using Domain.Entities;

namespace Application.Interfaces.Repositories.DrugStoreRepositories;

public interface IDrugStoreReadRepository : IReadRepository<DrugStore>
{
    public Task<DrugStore?> GetByNumberAndNetworkAsync(int number, string network, CancellationToken cancellationToken);
}