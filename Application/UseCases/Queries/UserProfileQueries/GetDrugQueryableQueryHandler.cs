using Application.Interfaces.Repositories.UserProfileRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.UserProfileQueries;

public class GetUserProfileQueryableQueryHandler : IRequestHandler<GetUserProfileQueryableQuery,IQueryable<UserProfile>>
{
   private readonly IUserProfileReadRepository _userProfileReadRepository;

    public GetUserProfileQueryableQueryHandler(IUserProfileReadRepository userProfileReadRepository)
    {
        _userProfileReadRepository = userProfileReadRepository;
    }
   public async Task<IQueryable<UserProfile>> Handle(GetUserProfileQueryableQuery request, CancellationToken cancellationToken)
   {
      var response = await _userProfileReadRepository.GetQueryableAsync(request.QueryOptions, cancellationToken);
      return response;
   }
}