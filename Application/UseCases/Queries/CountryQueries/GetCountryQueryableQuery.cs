using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.CountryQueries;

public record GetCountryQueryableQuery() : IRequest<IQueryable<Country>>;