using Application.Interfaces.Repositories.BaseRepositories;
using Domain.Entities;

namespace Application.Interfaces.Repositories.CountryRepositories;

public interface ICountryReadRepository: IReadRepository<Country>
{
    Task<Country?> GetByCodeAsync(string countryCode, CancellationToken cancellationToken);
}