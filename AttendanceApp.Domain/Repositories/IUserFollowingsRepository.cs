using AttendanceApp.Domain.Users;

namespace AttendanceApp.Domain.Repositories;

public interface IUserFollowingsRepository : IRepository<UserFollowing>
{
    Task<UserFollowing?> GetFollowingAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
}