using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugStoreQueries;

public class GetDrugStoreByNumberAndNetworkQueryHandler(IDrugStoreReadRepository readRepository)
    : IRequestHandler<GetDrugStoreByNumberAndNetworkQuery, DrugStore?>
{
    public async Task<DrugStore?> Handle(GetDrugStoreByNumberAndNetworkQuery request, CancellationToken cancellationToken)
    {
        return await readRepository.GetByNumberAndNetworkAsync(request.Number, request.Network, cancellationToken);
    }
}