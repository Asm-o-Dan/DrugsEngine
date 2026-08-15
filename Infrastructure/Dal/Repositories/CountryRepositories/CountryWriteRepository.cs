using Application.Interfaces.Repositories.CountryRepositories;
using Infrastructure.Dal.Repositories.BaseRepositories;
using ISO3166;
using Country = Domain.Entities.Country;

namespace Infrastructure.Dal.Repositories.CountryRepositories;

public class CountryWriteRepository(DrugsBotDbContext dbContext) : WriteRepository<Country>(dbContext),ICountryWriteRepository;