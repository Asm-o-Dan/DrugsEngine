using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.CountryRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.CountryQueries;

public class GetCountryByIdQueryHandler: IRequestHandler<GetCountryByIdQuery,Country?>
{
    private readonly ICountryReadRepository _countryReadRepository;

    public GetCountryByIdQueryHandler(ICountryReadRepository countryReadRepository)
    {
        _countryReadRepository = countryReadRepository;
    }
    public async Task<Country?> Handle(GetCountryByIdQuery request, CancellationToken cancellationToken)
    {
        var response = await _countryReadRepository.GetByIdAsync(request.Id,cancellationToken);
        
        return response;
    }
}
//TODO: Для каждой сущности(CountryCountryStore) кроме базовой сделать CRUD , попробовать сделать для CountryItem,FavoriteCountry
//TODO : UpdateCountryCommand
//TODO: read about primary constructor