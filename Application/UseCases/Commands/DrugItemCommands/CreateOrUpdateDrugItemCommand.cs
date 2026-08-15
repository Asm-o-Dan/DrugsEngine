using System.Windows.Input;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugItemCommands;

public record CreateOrUpdateDrugItemCommand(DrugItem DrugItem) : IRequest<Unit>;