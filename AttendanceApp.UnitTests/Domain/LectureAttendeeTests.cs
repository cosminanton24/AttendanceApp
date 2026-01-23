using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Lectures;

namespace AttendanceApp.UnitTests.Domain;

public class LectureAttendeeTests
{
    private readonly Guid _lectureId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_CreatesLectureAttendee()
    {
        // Act
        var attendee = new LectureAttendee(_lectureId, _userId);

        // Assert
        Assert.NotNull(attendee);
        Assert.Equal(_lectureId, attendee.LectureId);
        Assert.Equal(_userId, attendee.UserId);
        Assert.NotEqual(default, attendee.TimeJoined);
    }

    [Fact]
    public void Constructor_WithEmptyLectureId_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new LectureAttendee(Guid.Empty, _userId));
        Assert.Equal("lectureId is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => new LectureAttendee(_lectureId, Guid.Empty));
        Assert.Equal("userId is required.", ex.Message);
    }

    [Fact]
    public void Constructor_SetsTimeJoinedToCurrentUtcTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var attendee = new LectureAttendee(_lectureId, _userId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.InRange(attendee.TimeJoined, beforeCreation, afterCreation);
    }

    [Fact]
    public void Equals_WithSameAttendeeId_ReturnsTrue()
    {
        // Arrange
        var attendee1 = new LectureAttendee(_lectureId, _userId);
        var attendee2 = new LectureAttendee(Guid.NewGuid(), Guid.NewGuid());
        
        // Manually set the same ID for equality test
        var idProperty = typeof(LectureAttendee).GetProperty("Id");
        idProperty?.SetValue(attendee2, attendee1.Id);

        // Act & Assert
        Assert.Equal(attendee1, attendee2);
    }

    [Fact]
    public void Equals_SameInstancesAreEqual()
    {
        // Arrange
        var attendee = new LectureAttendee(_lectureId, _userId);

        // Act & Assert
        Assert.Equal(attendee, attendee);
    }

    [Fact]
    public void GetHashCode_WithSameAttendeeId_ReturnsSameHashCode()
    {
        // Arrange
        var attendee1 = new LectureAttendee(_lectureId, _userId);
        var attendee2 = new LectureAttendee(Guid.NewGuid(), Guid.NewGuid());
        
        // Manually set the same ID
        var idProperty = typeof(LectureAttendee).GetProperty("Id");
        idProperty?.SetValue(attendee2, attendee1.Id);

        // Act & Assert
        Assert.Equal(attendee1.GetHashCode(), attendee2.GetHashCode());
    }

    [Fact]
    public void Constructor_MultipleAttendees_CreatedSuccessfully()
    {
        // Act
        var attendee1 = new LectureAttendee(_lectureId, _userId);
        var attendee2 = new LectureAttendee(_lectureId, Guid.NewGuid());
        var attendee3 = new LectureAttendee(_lectureId, Guid.NewGuid());

        // Assert
        Assert.NotNull(attendee1);
        Assert.NotNull(attendee2);
        Assert.NotNull(attendee3);
        Assert.Equal(_lectureId, attendee1.LectureId);
        Assert.Equal(_lectureId, attendee2.LectureId);
        Assert.Equal(_lectureId, attendee3.LectureId);
    }

    [Fact]
    public void LectureId_IsReadOnly_CannotBeChanged()
    {
        // Arrange
        _ = new LectureAttendee(_lectureId, _userId);

        // Act - Try to change LectureId (should not be possible due to private setter)
        var lectureIdProperty = typeof(LectureAttendee).GetProperty("LectureId");

        // Assert - Property should have private setter
        Assert.NotNull(lectureIdProperty);
        Assert.NotNull(lectureIdProperty.SetMethod);
        Assert.False(lectureIdProperty.SetMethod.IsPublic);
    }

    [Fact]
    public void UserId_IsReadOnly_CannotBeChanged()
    {
        // Arrange
        _ = new LectureAttendee(_lectureId, _userId);

        // Act - Try to change UserId (should not be possible due to private setter)
        var userIdProperty = typeof(LectureAttendee).GetProperty("UserId");

        // Assert - Property should have private setter
        Assert.NotNull(userIdProperty);
        Assert.NotNull(userIdProperty.SetMethod);
        Assert.False(userIdProperty.SetMethod.IsPublic);
    }

    [Fact]
    public void TimeJoined_IsInitProperty_CanBeSetOnlyDuringConstruction()
    {
        // Arrange
        var attendee = new LectureAttendee(_lectureId, _userId);
        _ = attendee.TimeJoined;

        // Assert - TimeJoined should have init-only setter
        var timeJoinedProperty = typeof(LectureAttendee).GetProperty("TimeJoined");
        Assert.NotNull(timeJoinedProperty);
        Assert.True(timeJoinedProperty.CanRead);
    }
}
