using Application.Interfaces;
using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.FavoriteDrugCommands;

public class UpdateFavoriteDrugCommandHandler : IRequestHandler<UpdateFavoriteDrugCommand,Unit>
{
    private readonly IFavoriteDrugWriteRepository _favoriteDrugWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFavoriteDrugCommandHandler(IFavoriteDrugWriteRepository favoriteDrugWriteRepository, IUnitOfWork unitOfWork)
    {
        _favoriteDrugWriteRepository = favoriteDrugWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateFavoriteDrugCommand request, CancellationToken cancellationToken)
    {
        await _favoriteDrugWriteRepository.UpdateAsync(request.FavoriteDrug, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value; 
    }
}