using MediatR;

namespace Application.UseCases.Commands.DrugCommands;

public record UpdateDrugCommand(
    Guid Id,
    string Name,
    string Manufacturer,
    string CountryCode,
    string CountryName
) : IRequest<Unit>;