using Application.Interfaces.Repositories.UserProfileRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.UserProfileQueries;

public class GetUserProfileQueryableQueryHandler(IUserProfileReadRepository userProfileReadRepository)
    : IRequestHandler<GetUserProfileQueryableQuery, IQueryable<UserProfile>>
{
    public async Task<IQueryable<UserProfile>> Handle(GetUserProfileQueryableQuery request, CancellationToken cancellationToken)
    {
        return await userProfileReadRepository.GetQueryableAsync(cancellationToken);
    }
}