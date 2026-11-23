using Application.Interfaces.Repositories.CountryRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.CountryQueries;

public class GetCountryQueryableQueryHandler : IRequestHandler<GetCountryQueryableQuery,IQueryable<Country>>
{
   private readonly ICountryReadRepository _countryReadRepository;

    public GetCountryQueryableQueryHandler(ICountryReadRepository countryReadRepository)
    {
        _countryReadRepository = countryReadRepository;
    }
   public async Task<IQueryable<Country>> Handle(GetCountryQueryableQuery request, CancellationToken cancellationToken)
   {
      var response = await _countryReadRepository.GetQueryableAsync(request.QueryOptions, cancellationToken);
      return response;
   }
}