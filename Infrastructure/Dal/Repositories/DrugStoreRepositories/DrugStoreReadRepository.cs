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
            .FirstOrDefaultAsync(dS => dS.Number == number && dS.DrugNetwork == network);
    }
}