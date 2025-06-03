using Application.Interfaces;
using Application.Interfaces.Repositories.CountryRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.CountryCommands;

public class CreateCountryCommandHandler : IRequestHandler<CreateCountryCommand,Unit>
{
    private readonly ICountryWriteRepository _countryWriteRepository;
    
    private readonly IUnitOfWork _unitOfWork;

    public CreateCountryCommandHandler(ICountryWriteRepository countryWriteRepository, IUnitOfWork unitOfWork)
    {
        _countryWriteRepository = countryWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CreateCountryCommand request, CancellationToken cancellationToken)
    {
        await _countryWriteRepository.AddAsync(request.Country,cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}