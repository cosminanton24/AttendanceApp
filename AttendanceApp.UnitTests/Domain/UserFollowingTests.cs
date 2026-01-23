using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Users;

namespace AttendanceApp.UnitTests.Domain;

public class UserFollowingTests
{
    private readonly Guid _followerId = Guid.NewGuid();
    private readonly Guid _followedId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesUserFollowing()
    {
        // Act
        var following = new UserFollowing(_followerId, _followedId);

        // Assert
        Assert.NotNull(following);
        Assert.Equal(_followerId, following.FollowerId);
        Assert.Equal(_followedId, following.FollowedId);
        Assert.NotEqual(default, following.FollowedAt);
    }

    [Fact]
    public void Constructor_WithEmptyFollowerId_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new UserFollowing(Guid.Empty, _followedId));
        Assert.Equal("followerId is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyFollowedId_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new UserFollowing(_followerId, Guid.Empty));
        Assert.Equal("followedId is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithFollowerIdEqualToFollowedId_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new UserFollowing(userId, userId));
        Assert.Equal("A user cannot follow themselves.", ex.Message);
    }

    [Fact]
    public void Constructor_SetsFollowedAtToCurrentUtcTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var following = new UserFollowing(_followerId, _followedId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.InRange(following.FollowedAt, beforeCreation, afterCreation);
    }

    [Fact]
    public void Equals_WithSameFollowingId_ReturnsTrue()
    {
        // Arrange
        var following1 = new UserFollowing(_followerId, _followedId);
        var following2 = new UserFollowing(Guid.NewGuid(), Guid.NewGuid());
        
        // Manually set the same ID for equality test
        var idProperty = typeof(UserFollowing).GetProperty("Id");
        idProperty?.SetValue(following2, following1.Id);

        // Act & Assert
        Assert.Equal(following1, following2);
    }

    [Fact]
    public void Equals_SameInstancesAreEqual()
    {
        // Arrange
        var following = new UserFollowing(_followerId, _followedId);

        // Act & Assert
        Assert.Equal(following, following);
    }

    [Fact]
    public void GetHashCode_WithSameFollowingId_ReturnsSameHashCode()
    {
        // Arrange
        var following1 = new UserFollowing(_followerId, _followedId);
        var following2 = new UserFollowing(Guid.NewGuid(), Guid.NewGuid());
        
        // Manually set the same ID
        var idProperty = typeof(UserFollowing).GetProperty("Id");
        idProperty?.SetValue(following2, following1.Id);

        // Act & Assert
        Assert.Equal(following1.GetHashCode(), following2.GetHashCode());
    }

    [Fact]
    public void Constructor_MultipleFollowings_CreatedSuccessfully()
    {
        // Act
        var following1 = new UserFollowing(_followerId, _followedId);
        var following2 = new UserFollowing(Guid.NewGuid(), Guid.NewGuid());
        var following3 = new UserFollowing(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.NotNull(following1);
        Assert.NotNull(following2);
        Assert.NotNull(following3);
        Assert.Equal(_followerId, following1.FollowerId);
        Assert.Equal(_followedId, following1.FollowedId);
    }

    [Fact]
    public void FollowerId_IsReadOnly_CannotBeChanged()
    {
        // Arrange
        _ = new UserFollowing(_followerId, _followedId);

        // Act - Check FollowerId property
        var followerIdProperty = typeof(UserFollowing).GetProperty("FollowerId");

        // Assert - Property should have private setter
        Assert.NotNull(followerIdProperty);
        Assert.NotNull(followerIdProperty.SetMethod);
        Assert.False(followerIdProperty.SetMethod.IsPublic);
    }

    [Fact]
    public void FollowedId_IsReadOnly_CannotBeChanged()
    {
        // Arrange
        _ = new UserFollowing(_followerId, _followedId);

        // Act - Check FollowedId property
        var followedIdProperty = typeof(UserFollowing).GetProperty("FollowedId");

        // Assert - Property should have private setter
        Assert.NotNull(followedIdProperty);
        Assert.NotNull(followedIdProperty.SetMethod);
        Assert.False(followedIdProperty.SetMethod.IsPublic);
    }

    [Fact]
    public void FollowedAt_IsReadOnly_CannotBeChanged()
    {
        // Arrange
        _ = new UserFollowing(_followerId, _followedId);

        // Act - Check FollowedAt property
        var followedAtProperty = typeof(UserFollowing).GetProperty("FollowedAt");

        // Assert - Property should have private setter
        Assert.NotNull(followedAtProperty);
        Assert.NotNull(followedAtProperty.SetMethod);
        Assert.False(followedAtProperty.SetMethod.IsPublic);
    }

    [Fact]
    public void Constructor_DifferentFollowerAndFollowedCombinations_CreatesSuccessfully()
    {
        // Act
        var following1 = new UserFollowing(Guid.NewGuid(), Guid.NewGuid());
        var following2 = new UserFollowing(Guid.NewGuid(), Guid.NewGuid());
        var following3 = new UserFollowing(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.NotEqual(following1.FollowerId, following2.FollowerId);
        Assert.NotEqual(following2.FollowedId, following3.FollowedId);
        Assert.NotEqual(following1.FollowerId, following1.FollowedId);
        Assert.NotEqual(following2.FollowerId, following2.FollowedId);
        Assert.NotEqual(following3.FollowerId, following3.FollowedId);
    }

    [Fact]
    public void Constructor_WithBothEmptyGuids_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new UserFollowing(Guid.Empty, Guid.Empty));
        // Will throw on the first empty guid check (followerId)
        Assert.Equal("followerId is required.", ex.Message);
    }

}
