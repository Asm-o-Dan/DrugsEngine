using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.UserProfileQueries;

public record GetUserProfileQueryableQuery() : IRequest<IQueryable<UserProfile>>;