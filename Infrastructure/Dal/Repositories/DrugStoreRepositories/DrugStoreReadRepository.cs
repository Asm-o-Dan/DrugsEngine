using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dal.Repositories.DrugStoreRepositories;

public class DrugStoreReadRepository(DrugsBotDbContext dbContext)
    : ReadRepository<DrugStore>(dbContext), IDrugStoreReadRepository
{
    public async Task<DrugStore?> GetByNumberAndNetworkAsync(int number, string network, CancellationToken cancellationToken)
    {
        return await _dbContext.DrugStores.AsNoTracking()
            .FirstOrDefaultAsync(dS => dS.Number == number && dS.DrugNetwork == network, cancellationToken);
    }

    public async Task<DrugStore?> GetByExternalKeyAsync(string network, string externalKey, CancellationToken cancellationToken)
    {
        return await _dbContext.DrugStores.AsNoTracking()
            .FirstOrDefaultAsync(s => s.DrugNetwork == network && s.ExternalKey == externalKey, cancellationToken);
    }

    public async Task<int> GetMaxNumberAsync(string network, CancellationToken cancellationToken)
    {
        return await _dbContext.DrugStores
            .Where(s => s.DrugNetwork == network)
            .MaxAsync(s => (int?)s.Number, cancellationToken) ?? 0;
    }
}
