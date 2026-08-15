using Application.Interfaces.Repositories.CountryRepositories;
using Infrastructure.Dal.Repositories.BaseRepositories;
using ISO3166;
using Microsoft.EntityFrameworkCore;
using Country = Domain.Entities.Country;

namespace Infrastructure.Dal.Repositories.CountryRepositories;

public class CountryReadRepository(DrugsBotDbContext dbContext)
    : ReadRepository<Country>(dbContext), ICountryReadRepository
{
    public async Task<Country?> GetByCodeAsync(string countryCode, CancellationToken cancellationToken)
    {
        return await _dbContext.Countries.AsNoTracking().FirstOrDefaultAsync(x => x.Code == countryCode);
    }
}