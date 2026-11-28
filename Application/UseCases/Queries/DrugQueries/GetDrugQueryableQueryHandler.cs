using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugQueries;

public class GetDrugQueryableQueryHandler(IDrugReadRepository drugReadRepository)
    : IRequestHandler<GetDrugQueryableQuery, IQueryable<Drug>>
{
    public async Task<IQueryable<Drug>> Handle(GetDrugQueryableQuery request, CancellationToken cancellationToken)
    {
        return await drugReadRepository.GetQueryableAsync(cancellationToken);
    }
}