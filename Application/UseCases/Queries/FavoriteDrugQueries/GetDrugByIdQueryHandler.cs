using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.FavoriteDrugQueries;

public class GetFavoriteDrugByIdQueryHandler: IRequestHandler<GetFavoriteDrugByIdQuery,FavoriteDrug?>
{
    private readonly IFavoriteDrugReadRepository _favoriteDrugReadRepository;

    public GetFavoriteDrugByIdQueryHandler(IFavoriteDrugReadRepository favoriteDrugReadRepository)
    {
        _favoriteDrugReadRepository = favoriteDrugReadRepository;
    }
    public async Task<FavoriteDrug?> Handle(GetFavoriteDrugByIdQuery request, CancellationToken cancellationToken)
    {
        var response = await _favoriteDrugReadRepository.GetByIdAsync(request.Id,cancellationToken);
        
        return response;
    }
}
//TODO: Для каждой сущности(CountryFavoriteDrugFavoriteDrugStore) кроме базовой сделать CRUD , попробовать сделать для FavoriteDrugItem,FavoriteFavoriteDrug
//TODO : UpdateFavoriteDrugCommand
//TODO: read about primary constructor