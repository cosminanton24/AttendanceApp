using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.UnitTests.Domain;

public class QuizLectureTests
{
    private readonly Guid _lectureId = Guid.NewGuid();
    private readonly Guid _quizId = Guid.NewGuid();
    private readonly DateTime _createdAtUtc = DateTime.UtcNow;
    private readonly TimeSpan _quizDuration = TimeSpan.FromMinutes(30);

    #region Create Tests

    [Fact]
    public void Create_WithValidData_CreatesQuizLecture()
    {
        // Act
        var quizLecture = QuizLecture.Create(_lectureId, _quizId, _createdAtUtc, _quizDuration);

        // Assert
        Assert.NotEqual(Guid.Empty, quizLecture.Id);
        Assert.Equal(_lectureId, quizLecture.LectureId);
        Assert.Equal(_quizId, quizLecture.QuizId);
        Assert.Equal(_createdAtUtc, quizLecture.CreatedAtUtc);
        Assert.Equal(_createdAtUtc.Add(_quizDuration), quizLecture.EndTimeUtc);
    }

    [Fact]
    public void Create_WithEmptyLectureId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            QuizLecture.Create(Guid.Empty, _quizId, _createdAtUtc, _quizDuration));
    }

    [Fact]
    public void Create_WithEmptyQuizId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            QuizLecture.Create(_lectureId, Guid.Empty, _createdAtUtc, _quizDuration));
    }

    [Fact]
    public void Create_CalculatesEndTimeCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromHours(2);
        var startTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var quizLecture = QuizLecture.Create(_lectureId, _quizId, startTime, duration);

        // Assert
        var expectedEndTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expectedEndTime, quizLecture.EndTimeUtc);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_BeforeEndTime_ReturnsTrue()
    {
        // Arrange
        var quizLecture = QuizLecture.Create(_lectureId, _quizId, _createdAtUtc, _quizDuration);
        var checkTime = _createdAtUtc.AddMinutes(15); // Halfway through

        // Act
        var isActive = quizLecture.IsActive(checkTime);

        // Assert
        Assert.True(isActive);
    }

    [Fact]
    public void IsActive_AtStartTime_ReturnsTrue()
    {
        // Arrange
        var quizLecture = QuizLecture.Create(_lectureId, _quizId, _createdAtUtc, _quizDuration);

        // Act
        var isActive = quizLecture.IsActive(_createdAtUtc);

        // Assert
        Assert.True(isActive);
    }

    [Fact]
    public void IsActive_AtEndTime_ReturnsFalse()
    {
        // Arrange
        var quizLecture = QuizLecture.Create(_lectureId, _quizId, _createdAtUtc, _quizDuration);
        var endTime = _createdAtUtc.Add(_quizDuration);

        // Act
        var isActive = quizLecture.IsActive(endTime);

        // Assert
        Assert.False(isActive);
    }

    [Fact]
    public void IsActive_AfterEndTime_ReturnsFalse()
    {
        // Arrange
        var quizLecture = QuizLecture.Create(_lectureId, _quizId, _createdAtUtc, _quizDuration);
        var afterEndTime = _createdAtUtc.Add(_quizDuration).AddMinutes(1);

        // Act
        var isActive = quizLecture.IsActive(afterEndTime);

        // Assert
        Assert.False(isActive);
    }

    [Fact]
    public void IsActive_LongAfterEndTime_ReturnsFalse()
    {
        // Arrange
        var quizLecture = QuizLecture.Create(_lectureId, _quizId, _createdAtUtc, _quizDuration);
        var longAfter = _createdAtUtc.AddDays(1);

        // Act
        var isActive = quizLecture.IsActive(longAfter);

        // Assert
        Assert.False(isActive);
    }

    [Fact]
    public void IsActive_JustBeforeEndTime_ReturnsTrue()
    {
        // Arrange
        var quizLecture = QuizLecture.Create(_lectureId, _quizId, _createdAtUtc, _quizDuration);
        var justBefore = _createdAtUtc.Add(_quizDuration).AddMilliseconds(-1);

        // Act
        var isActive = quizLecture.IsActive(justBefore);

        // Assert
        Assert.True(isActive);
    }

    #endregion
}
