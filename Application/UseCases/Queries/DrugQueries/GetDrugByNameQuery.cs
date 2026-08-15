using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugQueries;

public record GetDrugByNameQuery(string Name) : IRequest<Drug?>;