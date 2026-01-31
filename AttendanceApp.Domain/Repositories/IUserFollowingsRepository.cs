using AttendanceApp.Domain.Users;

namespace AttendanceApp.Domain.Repositories;

public interface IUserFollowingsRepository : IRepository<UserFollowing>
{
    Task<UserFollowing?> GetFollowingAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);

    Task<int> GetTotalFollowingAsync(Guid followerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetFollowingUserIdsAsync(Guid followerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetTotalFollowersAsync(Guid followedId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetFollowerUserIdsAsync(Guid followedId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}