using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.UserProfileRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Queries.UserProfileQueries;

public class GetUserProfileByIdQueryHandler: IRequestHandler<GetUserProfileByIdQuery,UserProfile?>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;

    public GetUserProfileByIdQueryHandler(IUserProfileReadRepository userProfileReadRepository)
    {
        _userProfileReadRepository = userProfileReadRepository;
    }
    public async Task<UserProfile?> Handle(GetUserProfileByIdQuery request, CancellationToken cancellationToken)
    {
        var response = await _userProfileReadRepository.GetByIdAsync(request.Id,cancellationToken);
        
        return response;
    }
}
//TODO: Для каждой сущности(CountryUserProfileUserProfileStore) кроме базовой сделать CRUD , попробовать сделать для UserProfileItem,FavoriteUserProfile
//TODO : UpdateUserProfileCommand
//TODO: read about primary constructor