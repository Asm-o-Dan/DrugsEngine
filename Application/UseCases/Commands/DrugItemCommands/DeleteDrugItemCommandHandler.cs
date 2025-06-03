using Application.Interfaces;
using Application.Interfaces.Repositories.DrugItemRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugItemCommands;

public class DeleteDrugItemCommandHandler: IRequestHandler<DeleteDrugItemCommand,Unit>
{
    private readonly IDrugItemWriteRepository _drugItemWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDrugItemCommandHandler(IDrugItemWriteRepository drugItemWriteRepository, IUnitOfWork unitOfWork)
    {
        _drugItemWriteRepository = drugItemWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteDrugItemCommand request, CancellationToken cancellationToken)
    {
        await _drugItemWriteRepository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}