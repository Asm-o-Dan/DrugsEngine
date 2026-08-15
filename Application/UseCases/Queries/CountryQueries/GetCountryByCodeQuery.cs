using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.CountryQueries;

public record GetCountryByCodeQuery(string Code) : IRequest<Country?>;