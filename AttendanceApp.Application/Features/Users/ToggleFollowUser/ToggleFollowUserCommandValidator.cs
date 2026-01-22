
using System.ComponentModel.DataAnnotations;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Domain.Users;
using MediatR;

namespace AttendanceApp.Application.Features.Users.ToggleFollowUser;

public class ToggleFollowUserCommandHandler(IUserRepository _userRepo, IUserFollowingsRepository _userFollowingsRepository) : IRequestHandler<ToggleFollowUserCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ToggleFollowUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with ID {command.UserId} found.");

        var target = await _userRepo.GetByIdAsync(command.TargetId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with ID {command.TargetId} found.");

        if(target.Type != Domain.Enums.UserType.Professor)
            throw new ValidationException($"You can only follow professors");

        var followState = await _userFollowingsRepository.GetFollowingAsync(user.Id, target.Id, cancellationToken);
        bool newFollowState;
        
        if (followState != null)
        {
            await _userFollowingsRepository.DeleteByIdAsync(followState.Id, cancellationToken);
            newFollowState = false;
        }
        else
        {
            var newFollowing = new UserFollowing
            (
                user.Id,
                target.Id
            );
            await _userFollowingsRepository.AddAsync(newFollowing, cancellationToken);
            newFollowState = true;
        }

        await _userFollowingsRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(newFollowState);
    }
}