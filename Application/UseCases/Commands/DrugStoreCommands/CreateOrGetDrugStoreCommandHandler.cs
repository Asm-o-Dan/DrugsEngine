using Application.Interfaces;
using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Commands.DrugStoreCommands;

public class CreateOrGetDrugStoreCommandHandler : IRequestHandler<CreateOrGetDrugStoreCommand, Guid>
{
    private readonly IDrugStoreReadRepository _drugStoreReadRepository;
    private readonly IDrugStoreWriteRepository _drugStoreWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrGetDrugStoreCommandHandler> _logger;

    public CreateOrGetDrugStoreCommandHandler(
        IDrugStoreReadRepository drugStoreReadRepository,
        IDrugStoreWriteRepository drugStoreWriteRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateOrGetDrugStoreCommandHandler> logger)
    {
        _drugStoreReadRepository = drugStoreReadRepository;
        _drugStoreWriteRepository = drugStoreWriteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrGetDrugStoreCommand request, CancellationToken cancellationToken)
    {
        var drugStore = request.DrugStore;

        // Ищем аптеку
        var existingDrugStore = await _drugStoreReadRepository.GetByNumberAndNetworkAsync(
            drugStore.Number, drugStore.DrugNetwork, cancellationToken);

        if (existingDrugStore != null)
        {
            _logger.LogInformation("Аптека найдена: {DrugStoreNumber}, {DrugStoreNetwork}", existingDrugStore.Number,
                existingDrugStore.DrugNetwork);
            return existingDrugStore.Id; // Возвращаем ID существующей аптеки
        }

        _logger.LogInformation("Аптека не найдена, создаём новую.");
        await _drugStoreWriteRepository.AddAsync(drugStore, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return drugStore.Id; // Возвращаем ID новой аптеки
    }
}