using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Users.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Users.UserFollowings.GetFollowing;

public sealed class GetFollowingQueryHandler(
    IUserRepository userRepository,
    IUserFollowingsRepository userFollowingsRepository)
    : IRequestHandler<GetFollowingQuery, Result<UsersPageDto>>
{
    public async Task<Result<UsersPageDto>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with ID {request.UserId} found.");

        var total = await userFollowingsRepository.GetTotalFollowingAsync(user.Id, cancellationToken);
        var followingIds = await userFollowingsRepository.GetFollowingUserIdsAsync(
            user.Id,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        if (followingIds.Count == 0)
            return Result<UsersPageDto>.Ok(new UsersPageDto(Array.Empty<UserInfoDto>(), total));

        var users = await userRepository.GetByIdsAsync(followingIds, cancellationToken);
        var byId = users.ToDictionary(u => u.Id, u => new UserInfoDto(u.Id, u.Name, u.Email));

        var items = new List<UserInfoDto>(followingIds.Count);
        var missing = new HashSet<Guid>();

        foreach (var id in followingIds)
        {
            if (byId.TryGetValue(id, out var dto))
            {
                items.Add(dto);
            }
            else
            {
                missing.Add(id);
            }
        }

        if (missing.Count > 0)
            throw new KeyNotFoundException($"No account(s) found for id(s): {string.Join(", ", missing)}");

        return Result<UsersPageDto>.Ok(new UsersPageDto(items, total));
    }
}
