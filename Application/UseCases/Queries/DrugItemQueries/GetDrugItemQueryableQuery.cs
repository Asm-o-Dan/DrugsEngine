using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugItemQueries;

public record GetDrugItemQueryableQuery() : IRequest<IQueryable<DrugItem>>;