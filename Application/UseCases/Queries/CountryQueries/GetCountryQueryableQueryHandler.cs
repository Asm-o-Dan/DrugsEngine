using Application.Interfaces.Repositories.CountryRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.CountryQueries;

public class GetCountryQueryableQueryHandler(ICountryReadRepository countryReadRepository)
    : IRequestHandler<GetCountryQueryableQuery, IQueryable<Country>>
{
    public async Task<IQueryable<Country>> Handle(GetCountryQueryableQuery request, CancellationToken cancellationToken)
    {
        return await countryReadRepository.GetQueryableAsync(cancellationToken);
    }
}