using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugStoreQueries;

public class GetDrugStoreQueryableQueryHandler : IRequestHandler<GetDrugStoreQueryableQuery,IQueryable<DrugStore>>
{
   private readonly IDrugStoreReadRepository _drugStoreReadRepository;

    public GetDrugStoreQueryableQueryHandler(IDrugStoreReadRepository drugStoreReadRepository)
    {
        _drugStoreReadRepository = drugStoreReadRepository;
    }
   public async Task<IQueryable<DrugStore>> Handle(GetDrugStoreQueryableQuery request, CancellationToken cancellationToken)
   {
      var response = await _drugStoreReadRepository.GetQueryableAsync(request.QueryOptions, cancellationToken);
      return response;
   }
}