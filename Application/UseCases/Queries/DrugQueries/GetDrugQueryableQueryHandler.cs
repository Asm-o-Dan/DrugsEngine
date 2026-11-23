using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugQueries;

public class GetDrugQueryableQueryHandler : IRequestHandler<GetDrugQueryableQuery,IQueryable<Drug>>
{
   private readonly IDrugReadRepository _drugReadRepository;

    public GetDrugQueryableQueryHandler(IDrugReadRepository drugReadRepository)
    {
        _drugReadRepository = drugReadRepository;
    }
   public async Task<IQueryable<Drug>> Handle(GetDrugQueryableQuery request, CancellationToken cancellationToken)
   {
      var response = await _drugReadRepository.GetQueryableAsync(request.QueryOptions, cancellationToken);
      return response;
   }
}