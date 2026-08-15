using Application.Interfaces;
using Application.Interfaces.Repositories.CountryRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.CountryCommands;

public class CreateOrGetCountryCommandHandler : IRequestHandler<CreateOrGetCountryCommand, string>
{
    private readonly ICountryReadRepository _countryReadRepository;
    private readonly ICountryWriteRepository _countryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrGetCountryCommandHandler(ICountryReadRepository countryReadRepository, ICountryWriteRepository countryWriteRepository, IUnitOfWork unitOfWork)
    {
        _countryReadRepository = countryReadRepository;
        _countryWriteRepository = countryWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> Handle(CreateOrGetCountryCommand request, CancellationToken cancellationToken)
    {
        var country = request.Country;
        var existingCountry = await _countryReadRepository.GetByCodeAsync(country.Code, cancellationToken);
        if (existingCountry != null)
        {
            return existingCountry.Code;
        }
        await _countryWriteRepository.AddAsync(country, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return country.Code;
    }
}