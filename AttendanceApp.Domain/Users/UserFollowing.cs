using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Users;

public sealed class UserFollowing : Entity<Guid>
{
    public Guid FollowerId { get; private set; }
    public Guid FollowedId { get; private set; }
    public DateTime FollowedAt { get; private set; }

    private UserFollowing() { }

    public UserFollowing(Guid followerId, Guid followedId)
    {
        Guard.NotEmpty(followerId, nameof(followerId));
        Guard.NotEmpty(followedId, nameof(followedId));

        if (followerId == followedId)
            throw new DomainException("A user cannot follow themselves.");

        Id = Guid.NewGuid();
        FollowerId = followerId;
        FollowedId = followedId;
        FollowedAt = DateTime.UtcNow;
    }
}
