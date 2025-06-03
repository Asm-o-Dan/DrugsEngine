using Application.Interfaces;
using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugCommands;

public class UpdateDrugCommandHandler : IRequestHandler<UpdateDrugCommand,Unit>
{
    private readonly IDrugWriteRepository _drugWriteRepository;
    
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDrugCommandHandler(IDrugWriteRepository drugWriteRepository, IUnitOfWork unitOfWork)
    {
        _drugWriteRepository = drugWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateDrugCommand request, CancellationToken cancellationToken)
    {
        await _drugWriteRepository.UpdateAsync(request.Drug, cancellationToken);
        await _unitOfWork.SaveChangesAsync();
        return Unit.Value; 
    }
}