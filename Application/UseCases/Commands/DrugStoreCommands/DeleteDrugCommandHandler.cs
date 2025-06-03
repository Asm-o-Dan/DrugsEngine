using Application.Interfaces;
using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugStoreCommands;

public class DeleteDrugStoreCommandHandler: IRequestHandler<DeleteDrugStoreCommand,Unit>
{
    private readonly IDrugStoreWriteRepository _drugStoreWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDrugStoreCommandHandler(IDrugStoreWriteRepository drugStoreWriteRepository, IUnitOfWork unitOfWork)
    {
        _drugStoreWriteRepository = drugStoreWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteDrugStoreCommand request, CancellationToken cancellationToken)
    {
        await _drugStoreWriteRepository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}