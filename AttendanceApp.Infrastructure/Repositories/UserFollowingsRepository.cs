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

    public async Task<int> GetTotalFollowingAsync(Guid followerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().CountAsync(x => x.FollowerId == followerId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetFollowingUserIdsAsync(Guid followerId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 20;

        return await _dbSet
            .AsNoTracking()
            .Where(x => x.FollowerId == followerId)
            .OrderByDescending(x => x.FollowedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => x.FollowedId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalFollowersAsync(Guid followedId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().CountAsync(x => x.FollowedId == followedId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetFollowerUserIdsAsync(Guid followedId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 20;

        return await _dbSet
            .AsNoTracking()
            .Where(x => x.FollowedId == followedId)
            .OrderByDescending(x => x.FollowedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => x.FollowerId)
            .ToListAsync(cancellationToken);
    }
}