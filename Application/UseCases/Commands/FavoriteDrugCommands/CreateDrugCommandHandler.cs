using Application.Interfaces;
using Application.Interfaces.Repositories.FavoriteDrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.FavoriteDrugCommands;

public class CreateFavoriteDrugCommandHandler : IRequestHandler<CreateFavoriteDrugCommand,Unit>
{
    private readonly IFavoriteDrugWriteRepository _favoriteDrugWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFavoriteDrugCommandHandler(IFavoriteDrugWriteRepository favoriteDrugWriteRepository, IUnitOfWork unitOfWork)
    {
        _favoriteDrugWriteRepository = favoriteDrugWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CreateFavoriteDrugCommand request, CancellationToken cancellationToken)
    {
        await _favoriteDrugWriteRepository.AddAsync(request.FavoriteDrug,cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}