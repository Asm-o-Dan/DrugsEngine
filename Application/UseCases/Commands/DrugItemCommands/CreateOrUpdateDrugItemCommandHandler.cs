using Application.Interfaces;
using Application.Interfaces.Repositories.DrugItemRepositories;
using Application.UseCases.Commands.DrugCommands;
using Application.UseCases.Commands.DrugStoreCommands;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugItemCommands;

/// <summary>
/// Сохраняет цену препарата в аптеке из данных парсера.
/// Сначала получаем настоящие Id препарата и аптеки, и только потом ищем существующую цену:
/// Id сущностей, созданных парсером, на каждом проходе новые и в БД их нет.
/// </summary>
public class CreateOrUpdateDrugItemCommandHandler(
    IDrugItemReadRepository drugItemReadRepository,
    IDrugItemWriteRepository drugItemWriteRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator) : IRequestHandler<CreateOrUpdateDrugItemCommand, Unit>
{
    public async Task<Unit> Handle(CreateOrUpdateDrugItemCommand request, CancellationToken cancellationToken)
    {
        var parsed = request.DrugItem;
        var drugId = await mediator.Send(new CreateOrGetDrugCommand(parsed.Drug!), cancellationToken);
        var drugStoreId = await mediator.Send(new CreateOrGetDrugStoreCommand(parsed.DrugStore), cancellationToken);

        var existing = await drugItemReadRepository.GetByDrugAndPharmacyAsync(drugId, drugStoreId, cancellationToken);
        if (existing != null)
        {
            // Дату цены берём только от источника; без неё UpdateDrugCountAndCost сдвинет дату лишь при изменении цены.
            existing.UpdateDrugCountAndCost(parsed.Count, parsed.Cost, parsed.ReportedPriceUpdatedAt, parsed.StockLevel);
            await drugItemWriteRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            parsed.DrugId = drugId;
            parsed.DrugStoreId = drugStoreId;
            parsed.Drug = null;
            parsed.DrugStore = null;
            await drugItemWriteRepository.AddAsync(parsed, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
