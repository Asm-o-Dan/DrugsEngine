using Application.Interfaces;
using Application.Interfaces.Repositories.CountryRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.CountryCommands;

public class UpdateCountryCommandHandler : IRequestHandler<UpdateCountryCommand,Unit>
{
    private readonly ICountryWriteRepository _countryWriteRepository;
 
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Unit> Handle(UpdateCountryCommand request, CancellationToken cancellationToken)
    {
        await _countryWriteRepository.UpdateAsync(request.Country, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value; 
    }
}