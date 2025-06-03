using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.DrugStoreCommands;

public record CreateOrGetDrugStoreCommand(DrugStore? DrugStore) : IRequest<Guid>;
