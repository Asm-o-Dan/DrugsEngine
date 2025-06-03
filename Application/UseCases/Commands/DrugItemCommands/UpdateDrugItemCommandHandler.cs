using Application.Interfaces;
using Application.Interfaces.Repositories.DrugItemRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugItemCommands;

public class UpdateDrugItemCommandHandler : IRequestHandler<UpdateDrugItemCommand,Unit>
{
    private readonly IDrugItemWriteRepository _drugItemWriteRepository;
    
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDrugItemCommandHandler(IDrugItemWriteRepository drugItemWriteRepository, IUnitOfWork unitOfWork)
    {
        _drugItemWriteRepository = drugItemWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateDrugItemCommand request, CancellationToken cancellationToken)
    {
        await _drugItemWriteRepository.UpdateAsync(request.DrugItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value; 
    }
}