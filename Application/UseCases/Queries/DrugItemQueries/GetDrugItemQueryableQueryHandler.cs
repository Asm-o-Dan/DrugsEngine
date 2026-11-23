using Application.Interfaces.Repositories.DrugItemRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugItemQueries;

public class GetDrugItemQueryableQueryHandler : IRequestHandler<GetDrugItemQueryableQuery,IQueryable<DrugItem>>
{
   private readonly IDrugItemReadRepository _drugItemReadRepository;

    public GetDrugItemQueryableQueryHandler(IDrugItemReadRepository drugItemReadRepository)
    {
        _drugItemReadRepository = drugItemReadRepository;
    }
   public async Task<IQueryable<DrugItem>> Handle(GetDrugItemQueryableQuery request, CancellationToken cancellationToken)
   {
      var response = await _drugItemReadRepository.GetQueryableAsync(request.QueryOptions, cancellationToken);
      return response;
   }
}