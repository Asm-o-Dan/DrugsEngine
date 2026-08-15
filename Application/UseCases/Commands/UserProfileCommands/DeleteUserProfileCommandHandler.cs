using Application.Interfaces;
using Application.Interfaces.Repositories.UserProfileRepositories;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Commands.UserProfileCommands;

public class DeleteUserProfileCommandHandler: IRequestHandler<DeleteUserProfileCommand,Unit>
{
    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserProfileCommandHandler(IUserProfileWriteRepository userProfileWriteRepository, IUnitOfWork unitOfWork)
    {
        _userProfileWriteRepository = userProfileWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteUserProfileCommand request, CancellationToken cancellationToken)
    {
        await _userProfileWriteRepository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}