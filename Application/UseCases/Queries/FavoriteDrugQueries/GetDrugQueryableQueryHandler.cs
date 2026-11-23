using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.FavoriteDrugQueries;

public class GetFavoriteDrugQueryableQueryHandler : IRequestHandler<GetFavoriteDrugQueryableQuery,IQueryable<FavoriteDrug>>
{
   private readonly IFavoriteDrugReadRepository _favoriteDrugReadRepository;

    public GetFavoriteDrugQueryableQueryHandler(IFavoriteDrugReadRepository favoriteDrugReadRepository)
    {
        _favoriteDrugReadRepository = favoriteDrugReadRepository;
    }
   public async Task<IQueryable<FavoriteDrug>> Handle(GetFavoriteDrugQueryableQuery request, CancellationToken cancellationToken)
   {
      var response = await _favoriteDrugReadRepository.GetQueryableAsync(request.QueryOptions, cancellationToken);
      return response;
   }
}