using Application.Interfaces;
using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugCommands;

public class DeleteDrugCommandHandler : IRequestHandler<DeleteDrugCommand, Unit>
{
    private readonly IDrugWriteRepository _drugWriteRepository;

    private readonly IUnitOfWork _unitOfWork;

    public DeleteDrugCommandHandler(IDrugWriteRepository drugWriteRepository, IUnitOfWork unitOfWork)
    {
        _drugWriteRepository = drugWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteDrugCommand request, CancellationToken cancellationToken)
    {
        await _drugWriteRepository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}