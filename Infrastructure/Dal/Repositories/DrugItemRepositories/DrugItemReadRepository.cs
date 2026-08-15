using Application.Interfaces.Repositories.DrugItemRepositories;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Dal.Repositories.BaseRepositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dal.Repositories.DrugItemRepositories;

public class DrugItemReadRepository(DrugsBotDbContext dbContext) : ReadRepository<DrugItem>(dbContext),IDrugItemReadRepository
{
    public async Task<DrugItem?> GetByDrugAndPharmacyAsync(Guid drugId, Guid drugStoreId, CancellationToken cancellationToken)
    {
        return await _dbContext.DrugItems.AsNoTracking().
            FirstOrDefaultAsync(item =>
            item.DrugId == drugId && item.DrugStoreId == drugStoreId);
    }
}
