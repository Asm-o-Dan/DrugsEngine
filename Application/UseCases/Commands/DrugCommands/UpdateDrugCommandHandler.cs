using Application.Interfaces;
using Application.Interfaces.Repositories.CountryRepositories;
using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugCommands;

public class UpdateDrugCommandHandler : IRequestHandler<UpdateDrugCommand, Unit>
{
    private readonly IDrugWriteRepository _drugWriteRepository;
    private readonly IDrugReadRepository _drugReadRepository;
    private readonly ICountryReadRepository _countryReadRepository;
    private readonly ICountryWriteRepository _countryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDrugCommandHandler(
        IDrugWriteRepository drugWriteRepository,
        IDrugReadRepository drugReadRepository,
        ICountryReadRepository countryReadRepository,
        ICountryWriteRepository countryWriteRepository,
        IUnitOfWork unitOfWork)
    {
        _drugWriteRepository = drugWriteRepository;
        _drugReadRepository = drugReadRepository;
        _countryReadRepository = countryReadRepository;
        _countryWriteRepository = countryWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateDrugCommand request, CancellationToken cancellationToken)
    {
        var existingDrug = await _drugReadRepository.GetByIdAsync(request.Id, cancellationToken);
        if (existingDrug == null)
        {
            throw new KeyNotFoundException($"Препарат с идентификатором {request.Id} не найден.");
        }

        Country? country = null;
        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            country = await _countryReadRepository.GetByCodeAsync(request.CountryCode, cancellationToken);
            if (country == null && !string.IsNullOrWhiteSpace(request.CountryName))
            {
                country = new Country(request.CountryName, request.CountryCode);
                await _countryWriteRepository.AddAsync(country, cancellationToken);
            }
        }

        existingDrug.UpdateDetails(request.Name, request.Manufacturer, request.CountryCode, country);
        await _drugWriteRepository.UpdateAsync(existingDrug, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}