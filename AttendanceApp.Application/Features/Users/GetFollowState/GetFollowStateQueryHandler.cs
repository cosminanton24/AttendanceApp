
using System.ComponentModel.DataAnnotations;
using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Users.GetFollowState;

public class GetFollowStateQueryHandler(IUserRepository _userRepo, IUserFollowingsRepository _userFollowingsRepository) : IRequestHandler<GetFollowStateQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(GetFollowStateQuery command, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with ID {command.UserId} found.");

        var target = await _userRepo.GetByIdAsync(command.TargetId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with ID {command.TargetId} found.");

        if(target.Type != Domain.Enums.UserType.Professor)
            throw new ValidationException($"You can only follow professors");

        var isFollowing = await _userFollowingsRepository.GetFollowingAsync(user.Id, target.Id, cancellationToken) != null;

        return Result<bool>.Ok(isFollowing);
    }
}