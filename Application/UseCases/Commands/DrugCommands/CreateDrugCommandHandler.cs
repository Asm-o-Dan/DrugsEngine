using Application.Interfaces;
using Application.Interfaces.Repositories.CountryRepositories;
using Application.Interfaces.Repositories.DrugRepositories;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using EdmModel = Microsoft.Data.Edm.Library.EdmModel;

namespace Application.UseCases.Commands.DrugCommands;

public class CreateDrugCommandHandler : IRequestHandler<CreateDrugCommand,Unit>
{
    private readonly IDrugWriteRepository _drugWriteRepository;
    private readonly ICountryReadRepository _countryReadRepository;
    private readonly ICountryWriteRepository _countryWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDrugCommandHandler(IDrugWriteRepository drugWriteRepository, IUnitOfWork unitOfWork, ICountryReadRepository countryReadRepository, ICountryWriteRepository countryWriteRepository)
    {
        _drugWriteRepository = drugWriteRepository;
        _unitOfWork = unitOfWork;
        _countryReadRepository = countryReadRepository;
        _countryWriteRepository = countryWriteRepository;
    }

    public async Task<Unit> Handle(CreateDrugCommand request, CancellationToken cancellationToken)
    {
        var drug = request.Drug; 
        // Проверяем, есть ли страна с таким кодом
        var existingCountry = await _countryReadRepository.GetByCodeAsync(drug.Country.Code, cancellationToken);

        // Если страна не найдена, создаем новую
        if (existingCountry == null)
        {
            var newCountry = new Country(drug.Country.Name,drug.Country.Code);
        
            // Сохраняем новую страну в базе данных
            await _countryWriteRepository.AddAsync(newCountry, cancellationToken);
        
            // Привязываем drug к новой стране
            drug.Country = newCountry;
        }
        else
        {
            // Если страна найдена, просто привязываем drug к существующей стране
            drug.Country = existingCountry;
        }

        // Теперь сохраняем drug в базе данных
        await _drugWriteRepository.AddAsync(drug, cancellationToken);

        await _unitOfWork.SaveChangesAsync();
        
        return Unit.Value;
    }
}