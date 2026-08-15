using Application.Interfaces;
using Application.Interfaces.Repositories.UserProfileRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.UserProfileCommands;

public class CreateUserProfileCommandHandler : IRequestHandler<CreateUserProfileCommand,Unit>
{
    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserProfileCommandHandler(IUserProfileWriteRepository userProfileWriteRepository, IUnitOfWork unitOfWork)
    {
        _userProfileWriteRepository = userProfileWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CreateUserProfileCommand request, CancellationToken cancellationToken)
    {
        await _userProfileWriteRepository.AddAsync(request.UserProfile,cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}