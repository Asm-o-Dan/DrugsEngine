using Application.Interfaces;
using Application.Interfaces.Repositories.DrugItemRepositories;
using Application.UseCases.Commands.DrugCommands;
using Application.UseCases.Commands.DrugStoreCommands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Commands.DrugItemCommands;

public class CreateOrUpdateDrugItemCommandHandler : IRequestHandler<CreateOrUpdateDrugItemCommand, Unit>
{
    private readonly IDrugItemReadRepository _drugItemReadRepository;
    private readonly IDrugItemWriteRepository _drugItemWriteRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrUpdateDrugItemCommandHandler> _logger;

    public CreateOrUpdateDrugItemCommandHandler(
        IDrugItemReadRepository drugItemReadRepository,
        IDrugItemWriteRepository drugItemWriteRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<CreateOrUpdateDrugItemCommandHandler> logger)
    {
        _drugItemReadRepository = drugItemReadRepository;
        _drugItemWriteRepository = drugItemWriteRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(CreateOrUpdateDrugItemCommand request, CancellationToken cancellationToken)
    {
        var drugItem = request.DrugItem;

        // Разрешаем идентификатор Drug перед поиском существующей записи
        var drugId = drugItem.DrugId != Guid.Empty
            ? drugItem.DrugId
            : (drugItem.Drug != null
                ? await _mediator.Send(new CreateOrGetDrugCommand(drugItem.Drug), cancellationToken)
                : Guid.Empty);

        // Разрешаем идентификатор DrugStore перед поиском существующей записи
        var drugStoreId = drugItem.DrugStoreId != Guid.Empty
            ? drugItem.DrugStoreId
            : (drugItem.DrugStore != null
                ? await _mediator.Send(new CreateOrGetDrugStoreCommand(drugItem.DrugStore), cancellationToken)
                : Guid.Empty);

        if (drugId == Guid.Empty || drugStoreId == Guid.Empty)
        {
            _logger.LogWarning("Не удалось разрешить DrugId или DrugStoreId для DrugItem.");
            return Unit.Value;
        }

        var existingDrugItem = await _drugItemReadRepository.GetByDrugAndPharmacyAsync(drugId, drugStoreId, cancellationToken);

        if (existingDrugItem != null)
        {
            _logger.LogDebug("Обновление существующего DrugItem (DrugId: {DrugId}, StoreId: {StoreId})", drugId, drugStoreId);
            existingDrugItem.UpdateDrugCountAndCost(drugItem.Count, drugItem.Cost);
            await _drugItemWriteRepository.UpdateAsync(existingDrugItem, cancellationToken);
        }
        else
        {
            _logger.LogDebug("Создание нового DrugItem (DrugId: {DrugId}, StoreId: {StoreId})", drugId, drugStoreId);
            drugItem.DrugId = drugId;
            drugItem.DrugStoreId = drugStoreId;
            drugItem.Drug = null;
            drugItem.DrugStore = null;
            await _drugItemWriteRepository.AddAsync(drugItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}