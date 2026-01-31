using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Users.Dtos;
using AttendanceApp.Domain.Repositories;
using MediatR;

namespace AttendanceApp.Application.Features.Users.UserFollowings.GetFollowers;

public sealed class GetFollowersQueryHandler(
    IUserRepository userRepository,
    IUserFollowingsRepository userFollowingsRepository)
    : IRequestHandler<GetFollowersQuery, Result<UsersPageDto>>
{
    public async Task<Result<UsersPageDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"No account with ID {request.UserId} found.");

        var total = await userFollowingsRepository.GetTotalFollowersAsync(user.Id, cancellationToken);
        var followerIds = await userFollowingsRepository.GetFollowerUserIdsAsync(
            user.Id,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        if (followerIds.Count == 0)
            return Result<UsersPageDto>.Ok(new UsersPageDto(Array.Empty<UserInfoDto>(), total));

        var users = await userRepository.GetByIdsAsync(followerIds, cancellationToken);
        var byId = users.ToDictionary(u => u.Id, u => new UserInfoDto(u.Id, u.Name, u.Email));

        var items = new List<UserInfoDto>(followerIds.Count);
        var missing = new HashSet<Guid>();

        foreach (var id in followerIds)
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
