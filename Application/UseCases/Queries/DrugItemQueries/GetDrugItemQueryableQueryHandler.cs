using Application.Interfaces.Repositories.DrugItemRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugItemQueries;

public class GetDrugItemQueryableQueryHandler(IDrugItemReadRepository drugItemReadRepository)
    : IRequestHandler<GetDrugItemQueryableQuery, IQueryable<DrugItem>>
{
    public async Task<IQueryable<DrugItem>> Handle(GetDrugItemQueryableQuery request, CancellationToken cancellationToken)
    {
        return await drugItemReadRepository.GetQueryableAsync(cancellationToken);
    }
}