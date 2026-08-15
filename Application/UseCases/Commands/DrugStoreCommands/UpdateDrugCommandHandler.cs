using Application.Interfaces;
using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugStoreCommands;

public class UpdateDrugStoreCommandHandler : IRequestHandler<UpdateDrugStoreCommand,Unit>
{
    private readonly IDrugStoreWriteRepository _drugStoreWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDrugStoreCommandHandler(IDrugStoreWriteRepository drugStoreWriteRepository, IUnitOfWork unitOfWork)
    {
        _drugStoreWriteRepository = drugStoreWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateDrugStoreCommand request, CancellationToken cancellationToken)
    {
        await _drugStoreWriteRepository.UpdateAsync(request.DrugStore, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value; 
    }
}