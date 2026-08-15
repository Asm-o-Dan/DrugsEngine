using Application.Interfaces.Repositories.CountryRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.CountryQueries;

public class GetCountryByCodeQueryHandler(ICountryReadRepository countryReadRepository)
    : IRequestHandler<GetCountryByCodeQuery, Country?>
{
    private readonly ICountryReadRepository _countryReadRepository = countryReadRepository;

    public async Task<Country?> Handle(GetCountryByCodeQuery request, CancellationToken cancellationToken)
    {
        return await countryReadRepository.GetByCodeAsync(request.Code, cancellationToken);
    }
}