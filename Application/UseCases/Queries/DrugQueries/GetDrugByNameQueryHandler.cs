using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugQueries;

public class GetDrugByNameQueryHandler : IRequestHandler<GetDrugByNameQuery, Drug?>
{
    private readonly IDrugReadRepository _drugReadRepository;

    public GetDrugByNameQueryHandler(IDrugReadRepository drugReadRepository)
    {
        _drugReadRepository = drugReadRepository;
    }


    public async Task<Drug?> Handle(GetDrugByNameQuery request, CancellationToken cancellationToken)
    {
        return await _drugReadRepository.GetByNameAsync(request.Name, cancellationToken);
    }
}