using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugStoreQueries;

public class GetDrugStoreQueryableQueryHandler(IDrugStoreReadRepository drugStoreReadRepository)
    : IRequestHandler<GetDrugStoreQueryableQuery, IQueryable<DrugStore>>
{
    public async Task<IQueryable<DrugStore>> Handle(GetDrugStoreQueryableQuery request, CancellationToken cancellationToken)
    {
        return await drugStoreReadRepository.GetQueryableAsync(cancellationToken);
    }
}