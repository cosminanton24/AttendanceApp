using AttendanceApp.Domain.Repositories;
using AttendanceApp.Domain.Users;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class UserFollowingsRepository(AttendanceAppDbContext db) : GenericRepository<UserFollowing>(db), IUserFollowingsRepository
{
    public async Task<UserFollowing?> GetFollowingAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(uf => uf.FollowerId == followerId && uf.FollowedId == followedId, cancellationToken);
    }
}