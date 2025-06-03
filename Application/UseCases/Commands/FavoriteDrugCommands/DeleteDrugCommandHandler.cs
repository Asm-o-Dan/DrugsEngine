using Application.Interfaces;
using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.FavoriteDrugCommands;

public class DeleteFavoriteDrugCommandHandler: IRequestHandler<DeleteFavoriteDrugCommand,Unit>
{
    private readonly IFavoriteDrugWriteRepository _favoriteDrugWriteRepository;
    
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFavoriteDrugCommandHandler(IFavoriteDrugWriteRepository favoriteDrugWriteRepository, IUnitOfWork unitOfWork)
    {
        _favoriteDrugWriteRepository = favoriteDrugWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteFavoriteDrugCommand request, CancellationToken cancellationToken)
    {
        await _favoriteDrugWriteRepository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}