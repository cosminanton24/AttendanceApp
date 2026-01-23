using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Lectures;

namespace AttendanceApp.UnitTests.Domain;

public class LectureTests
{
    private readonly Guid _lectureId = Guid.NewGuid();
    private readonly Guid _professorId = Guid.NewGuid();
    private readonly string _name = "Introduction to C#";
    private readonly string _description = "A comprehensive course on C# fundamentals";
    private readonly DateTime _startTime = DateTime.UtcNow.AddHours(1);
    private readonly TimeSpan _duration = TimeSpan.FromHours(2);

    [Fact]
    public void Constructor_WithValidData_CreatesLecture()
    {
        // Act
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);

        // Assert
        Assert.Equal(_lectureId, lecture.Id);
        Assert.Equal(_professorId, lecture.ProfessorId);
        Assert.Equal(_name, lecture.Name);
        Assert.Equal(_description, lecture.Description);
        Assert.Equal(_startTime, lecture.StartTime);
        Assert.Equal(_duration, lecture.Duration);
        Assert.Equal(LectureStatus.Scheduled, lecture.Status);
        Assert.Empty(lecture.Attendees);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Lecture(Guid.Empty, _professorId, _name, _description, _startTime, _duration));
        Assert.Equal("id is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithEmptyProfessorId_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Lecture(_lectureId, Guid.Empty, _name, _description, _startTime, _duration));
        Assert.Equal("professorId is required.", ex.Message);
    }

    [Fact]
    public void Constructor_WithPastStartTime_ThrowsDomainException()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Lecture(_lectureId, _professorId, _name, _description, pastTime, _duration));
        Assert.Equal("startTime cannot be in the past.", ex.Message);
    }

    [Fact]
    public void Constructor_WithZeroDuration_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Lecture(_lectureId, _professorId, _name, _description, _startTime, TimeSpan.Zero));
        Assert.Equal("duration must be positive.", ex.Message);
    }

    [Fact]
    public void Constructor_WithNegativeDuration_ThrowsDomainException()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Lecture(_lectureId, _professorId, _name, _description, _startTime, TimeSpan.FromHours(-1)));
        Assert.Equal("duration must be positive.", ex.Message);
    }

    [Fact]
    public void Start_WhenScheduled_ChangesStatusToInProgress()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);

        // Act
        lecture.Start();

        // Assert
        Assert.Equal(LectureStatus.InProgress, lecture.Status);
    }

    [Fact]
    public void Start_WhenInProgress_ThrowsDomainException()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => lecture.Start());
        Assert.Equal("Lecture can only be started when scheduled.", ex.Message);
    }

    [Fact]
    public void Start_WhenEnded_ThrowsDomainException()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();
        lecture.End();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => lecture.Start());
        Assert.Equal("Lecture can only be started when scheduled.", ex.Message);
    }

    [Fact]
    public void End_WhenInProgress_ChangesStatusToEnded()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();

        // Act
        lecture.End();

        // Assert
        Assert.Equal(LectureStatus.Ended, lecture.Status);
    }

    [Fact]
    public void End_WhenScheduled_ThrowsDomainException()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => lecture.End());
        Assert.Equal("Lecture can only be ended when in progress.", ex.Message);
    }

    [Fact]
    public void End_WhenAlreadyEnded_ThrowsDomainException()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();
        lecture.End();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => lecture.End());
        Assert.Equal("Lecture can only be ended when in progress.", ex.Message);
    }

    [Fact]
    public void AddAttendee_WhenLectureInProgress_AddsAttendee()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();
        var userId = Guid.NewGuid();

        // Act
        lecture.AddAttendee(userId);

        // Assert
        Assert.Single(lecture.Attendees);
        Assert.Contains(lecture.Attendees, a => a.UserId == userId);
    }

    [Fact]
    public void AddAttendee_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => lecture.AddAttendee(Guid.Empty));
        Assert.Equal("userId is required.", ex.Message);
    }

    [Fact]
    public void AddAttendee_WhenLectureNotInProgress_ThrowsDomainException()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        var userId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => lecture.AddAttendee(userId));
        Assert.Equal("Cannot add attendees unless lecture is in progress.", ex.Message);
    }

    [Fact]
    public void AddAttendee_WhenLectureEnded_ThrowsDomainException()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();
        lecture.End();
        var userId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => lecture.AddAttendee(userId));
        Assert.Equal("Cannot add attendees unless lecture is in progress.", ex.Message);
    }

    [Fact]
    public void AddAttendee_SameUserMultipleTimes_AddsOnlyOnce()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();
        var userId = Guid.NewGuid();

        // Act
        lecture.AddAttendee(userId);
        lecture.AddAttendee(userId);

        // Assert
        Assert.Single(lecture.Attendees);
    }

    [Fact]
    public void AddAttendee_MultipleUsers_AddsAllAttendees()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        lecture.Start();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();

        // Act
        lecture.AddAttendee(userId1);
        lecture.AddAttendee(userId2);
        lecture.AddAttendee(userId3);

        // Assert
        Assert.Equal(3, lecture.Attendees.Count);
        Assert.Contains(lecture.Attendees, a => a.UserId == userId1);
        Assert.Contains(lecture.Attendees, a => a.UserId == userId2);
        Assert.Contains(lecture.Attendees, a => a.UserId == userId3);
    }

    [Fact]
    public void ChangeStatus_WithValidStatus_UpdatesStatus()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);

        // Act
        lecture.ChangeStatus(LectureStatus.Canceled);

        // Assert
        Assert.Equal(LectureStatus.Canceled, lecture.Status);
    }

    [Fact]
    public void ChangeStatus_ToMultipleStatuses_UpdatesCorrectly()
    {
        // Arrange
        var lecture = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);

        // Act & Assert
        lecture.ChangeStatus(LectureStatus.InProgress);
        Assert.Equal(LectureStatus.InProgress, lecture.Status);

        lecture.ChangeStatus(LectureStatus.Ended);
        Assert.Equal(LectureStatus.Ended, lecture.Status);

        lecture.ChangeStatus(LectureStatus.Canceled);
        Assert.Equal(LectureStatus.Canceled, lecture.Status);
    }

    [Fact]
    public void Equals_WithSameLectureId_ReturnsTrue()
    {
        // Arrange
        var lecture1 = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        var lecture2 = new Lecture(_lectureId, Guid.NewGuid(), "Different Name", "Different Description", 
            DateTime.UtcNow.AddHours(2), TimeSpan.FromHours(1));

        // Act & Assert
        Assert.Equal(lecture1, lecture2);
    }

    [Fact]
    public void Equals_WithDifferentLectureId_ReturnsFalse()
    {
        // Arrange
        var lecture1 = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        var lecture2 = new Lecture(Guid.NewGuid(), _professorId, _name, _description, _startTime, _duration);

        // Act & Assert
        Assert.NotEqual(lecture1, lecture2);
    }

    [Fact]
    public void GetHashCode_WithSameLectureId_ReturnsSameHashCode()
    {
        // Arrange
        var lecture1 = new Lecture(_lectureId, _professorId, _name, _description, _startTime, _duration);
        var lecture2 = new Lecture(_lectureId, Guid.NewGuid(), "Different Name", "Different Description",
            DateTime.UtcNow.AddHours(2), TimeSpan.FromHours(1));

        // Act & Assert
        Assert.Equal(lecture1.GetHashCode(), lecture2.GetHashCode());
    }
}
