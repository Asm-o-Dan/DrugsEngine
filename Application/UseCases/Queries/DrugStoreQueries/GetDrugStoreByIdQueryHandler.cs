using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.DrugStoreRepositories;
using Application.UseCases.Queries.DrugQueries;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugStoreQueries;

public class GetDrugByIdQueryHandler: IRequestHandler<GetDrugStoreByIdQuery,DrugStore?>
{
    private readonly IDrugStoreReadRepository _drugStoreReadRepository;

    public GetDrugByIdQueryHandler(IDrugStoreReadRepository drugStoreReadRepository)
    {
        _drugStoreReadRepository = drugStoreReadRepository;
    }
    public async Task<DrugStore?> Handle(GetDrugStoreByIdQuery request, CancellationToken cancellationToken)
    {
        var response = await _drugStoreReadRepository.GetByIdAsync(request.Id,cancellationToken);
        
        return response;
    }
}