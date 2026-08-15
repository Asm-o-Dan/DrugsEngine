using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.CountryCommands;

public record CreateOrGetCountryCommand(Country? Country):IRequest<string>;