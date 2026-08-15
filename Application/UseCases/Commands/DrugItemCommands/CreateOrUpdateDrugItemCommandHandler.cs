using Application.Interfaces;
using Application.Interfaces.Repositories.DrugItemRepositories;
using Application.UseCases.Commands.DrugCommands;
using Application.UseCases.Commands.DrugStoreCommands;
using Application.UseCases.Queries.DrugItemQueries;
using MediatR;

namespace Application.UseCases.Commands.DrugItemCommands;

public class CreateOrUpdateDrugItemCommandHandler : IRequestHandler<CreateOrUpdateDrugItemCommand, Unit>
{

    private readonly IDrugItemReadRepository _drugItemReadRepository;
    private readonly IDrugItemWriteRepository _drugItemWriteRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    public CreateOrUpdateDrugItemCommandHandler(IDrugItemReadRepository drugItemReadRepository, IDrugItemWriteRepository drugItemWriteRepository, IUnitOfWork unitOfWork, IMediator mediator)
    {
        _drugItemReadRepository = drugItemReadRepository;
        _drugItemWriteRepository = drugItemWriteRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CreateOrUpdateDrugItemCommand request, CancellationToken cancellationToken)
    {
        var drugItem = request.DrugItem;
        var existingDrugItem = await _drugItemReadRepository.GetByDrugAndPharmacyAsync(drugItem.DrugId, drugItem.DrugStoreId, cancellationToken);

        if (existingDrugItem != null)
        {
            // Обновление сущности
            
            existingDrugItem.UpdateDrugCountAndCost(drugItem.Count,drugItem.Cost);
            await _drugItemWriteRepository.UpdateAsync(existingDrugItem, cancellationToken);
        }
        else
        {
            // Добавление нового элемента
            drugItem.DrugId = await _mediator.Send(new CreateOrGetDrugCommand(drugItem.Drug),cancellationToken);
            drugItem.DrugStoreId = await _mediator.Send(new CreateOrGetDrugStoreCommand(drugItem.DrugStore),cancellationToken);
            drugItem.DrugStore = null;
            drugItem.Drug = null;
            await _drugItemWriteRepository.AddAsync(drugItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}