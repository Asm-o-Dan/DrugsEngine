using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.FavoriteDrugQueries;

public record GetFavoriteDrugQueryableQuery() : IRequest<IQueryable<FavoriteDrug>>;