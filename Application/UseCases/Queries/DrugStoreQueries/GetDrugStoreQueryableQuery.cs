using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.DrugStoreQueries;

public record GetDrugStoreQueryableQuery() : IRequest<IQueryable<DrugStore>>;