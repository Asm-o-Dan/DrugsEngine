using Application.Interfaces;
using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugStoreCommands;

public class CreateDrugStoreCommandHandler : IRequestHandler<CreateDrugStoreCommand,Unit>
{
    private readonly IDrugStoreWriteRepository _drugStoreWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDrugStoreCommandHandler(IDrugStoreWriteRepository drugStoreWriteRepository, IUnitOfWork unitOfWork)
    {
        _drugStoreWriteRepository = drugStoreWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CreateDrugStoreCommand request, CancellationToken cancellationToken)
    {
        await _drugStoreWriteRepository.AddAsync(request.DrugStore,cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}