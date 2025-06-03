using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugQueries;

public class GetDrugByIdQueryHandler(IDrugReadRepository drugReadRepository) : IRequestHandler<GetDrugByIdQuery, Drug?>
{
    public async Task<Drug?> Handle(GetDrugByIdQuery request, CancellationToken cancellationToken)
    {
        var response = await drugReadRepository.GetByIdAsync(request.Id,cancellationToken);
        
        return response;
    }
}
//TODO: Для каждой сущности(CountryDrugDrugStore) кроме базовой сделать CRUD , попробовать сделать для DrugItem,FavoriteDrug
//TODO : UpdateDrugCommand
//TODO: read about primary constructor