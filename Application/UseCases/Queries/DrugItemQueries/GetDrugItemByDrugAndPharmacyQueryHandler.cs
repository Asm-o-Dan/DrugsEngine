using Application.Interfaces.Repositories.DrugItemRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugItemQueries;

public class GetDrugItemByDrugAndPharmacyQueryHandler(IDrugItemReadRepository drugItemReadRepository)
    : IRequestHandler<GetDrugItemByDrugAndPharmacyQuery, DrugItem?>
{
    public async Task<DrugItem?> Handle(GetDrugItemByDrugAndPharmacyQuery request, CancellationToken cancellationToken)
    {
        return await drugItemReadRepository.GetByDrugAndPharmacyAsync(request.DrugId, request.DrugStoreId,
            cancellationToken);
    }
}