using Application.Interfaces;
using Application.Interfaces.Repositories.DrugItemRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugItemCommands;

public class CreateDrugItemCommandHandler : IRequestHandler<CreateDrugItemCommand, Unit>
{
    private readonly IDrugItemWriteRepository _drugWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDrugItemCommandHandler(IDrugItemWriteRepository drugWriteRepository, IUnitOfWork unitOfWork)
    {
        _drugWriteRepository = drugWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CreateDrugItemCommand request, CancellationToken cancellationToken)
    {
        await _drugWriteRepository.AddAsync(request.DrugItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}