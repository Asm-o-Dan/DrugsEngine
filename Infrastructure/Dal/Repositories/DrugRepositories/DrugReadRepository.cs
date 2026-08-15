using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using Infrastructure.Dal.Repositories.BaseRepositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dal.Repositories.DrugRepositories;

public class DrugReadRepository(DrugsBotDbContext dbContext) : ReadRepository<Drug>(dbContext), IDrugReadRepository
{
    public async Task<Drug?> GetByNameAsync(string drugName, CancellationToken cancellationToken)
    {
        return await _dbContext.Drugs.AsNoTracking().FirstOrDefaultAsync(x => x.Name == drugName);
    }
}