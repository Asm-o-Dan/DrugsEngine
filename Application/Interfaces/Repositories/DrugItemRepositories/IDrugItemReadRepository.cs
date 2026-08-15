using Application.Interfaces.Repositories.BaseRepositories;
using Domain.Entities;

namespace Application.Interfaces.Repositories.DrugItemRepositories;

public interface IDrugItemReadRepository : IReadRepository<DrugItem>
{
    Task<DrugItem?> GetByDrugAndPharmacyAsync(Guid drugId, Guid drugStoreId, CancellationToken cancellationToken);
}