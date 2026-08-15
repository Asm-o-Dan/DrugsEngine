using Application.Interfaces;
using Application.Interfaces.Repositories.UserProfileRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.UserProfileCommands;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand,Unit>
{
    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserProfileCommandHandler(IUserProfileWriteRepository userProfileWriteRepository, IUnitOfWork unitOfWork)
    {
        _userProfileWriteRepository = userProfileWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        await _userProfileWriteRepository.UpdateAsync(request.UserProfile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value; 
    }
}