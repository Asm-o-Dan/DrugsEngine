using Application.Interfaces;
using Application.Interfaces.Repositories.CountryRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.CountryCommands;

public class DeleteCountryCommandHandler: IRequestHandler<DeleteCountryCommand,Unit>
{
    private readonly ICountryWriteRepository _countryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCountryCommandHandler(ICountryWriteRepository countryWriteRepository, IUnitOfWork unitOfWork)
    {
        _countryWriteRepository = countryWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteCountryCommand request, CancellationToken cancellationToken)
    {
        await _countryWriteRepository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}