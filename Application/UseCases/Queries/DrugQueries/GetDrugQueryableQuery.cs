using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugQueries;

public record GetDrugQueryableQuery() : IRequest<IQueryable<Drug>>;