using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.FavoriteDrugQueries;

public class GetFavoriteDrugQueryableQueryHandler(IFavoriteDrugReadRepository favoriteDrugReadRepository)
    : IRequestHandler<GetFavoriteDrugQueryableQuery, IQueryable<FavoriteDrug>>
{
    public async Task<IQueryable<FavoriteDrug>> Handle(GetFavoriteDrugQueryableQuery request, CancellationToken cancellationToken)
    {
        return await favoriteDrugReadRepository.GetQueryableAsync(cancellationToken);
    }
}