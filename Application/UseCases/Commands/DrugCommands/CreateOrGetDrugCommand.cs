using System.Windows.Input;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugCommands;

public record CreateOrGetDrugCommand(Drug? Drug) : IRequest<Guid>;