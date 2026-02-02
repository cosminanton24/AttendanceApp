using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.UnitTests.Domain;

public class UserSubmissionTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _quizLectureId = Guid.NewGuid();
    private readonly DateTime _submittedAtUtc = DateTime.UtcNow;
    private readonly decimal _score = 85m;
    private readonly decimal _maxScore = 100m;

    #region Create Tests

    [Fact]
    public void Create_WithValidData_CreatesUserSubmission()
    {
        // Act
        var submission = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, _score, _maxScore);

        // Assert
        Assert.NotEqual(Guid.Empty, submission.Id);
        Assert.Equal(_userId, submission.UserId);
        Assert.Equal(_quizLectureId, submission.QuizLectureId);
        Assert.True(submission.Submitted);
        Assert.Equal(_submittedAtUtc, submission.SubmittedAtUtc);
        Assert.Equal(_score, submission.Score);
        Assert.Equal(_maxScore, submission.MaxScore);
    }

    [Fact]
    public void Create_SetsSubmittedToTrue()
    {
        // Act
        var submission = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, _score, _maxScore);

        // Assert
        Assert.True(submission.Submitted);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            UserSubmission.Create(Guid.Empty, _quizLectureId, _submittedAtUtc, _score, _maxScore));
    }

    [Fact]
    public void Create_WithEmptyQuizLectureId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            UserSubmission.Create(_userId, Guid.Empty, _submittedAtUtc, _score, _maxScore));
    }

    [Fact]
    public void Create_WithZeroScore_IsValid()
    {
        // Act
        var submission = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, 0m, _maxScore);

        // Assert
        Assert.Equal(0m, submission.Score);
    }

    [Fact]
    public void Create_WithZeroMaxScore_IsValid()
    {
        // Act
        var submission = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, _score, 0m);

        // Assert
        Assert.Equal(0m, submission.MaxScore);
    }

    [Fact]
    public void Create_WithPerfectScore_IsValid()
    {
        // Act
        var submission = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, 100m, 100m);

        // Assert
        Assert.Equal(100m, submission.Score);
        Assert.Equal(100m, submission.MaxScore);
    }

    [Fact]
    public void Create_WithDecimalScore_IsValid()
    {
        // Act
        var submission = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, 85.5m, 100m);

        // Assert
        Assert.Equal(85.5m, submission.Score);
    }

    [Fact]
    public void Create_WithLargeScore_IsValid()
    {
        // Act
        var submission = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, 1000m, 2000m);

        // Assert
        Assert.Equal(1000m, submission.Score);
        Assert.Equal(2000m, submission.MaxScore);
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        // Act
        var submission1 = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, _score, _maxScore);
        var submission2 = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, _score, _maxScore);

        // Assert
        Assert.NotEqual(submission1.Id, submission2.Id);
    }

    #endregion

    #region Score Calculation Tests

    [Theory]
    [InlineData(0, 100, 0)]        // 0%
    [InlineData(50, 100, 50)]      // 50%
    [InlineData(100, 100, 100)]    // 100%
    [InlineData(25, 50, 50)]       // 50% (25/50)
    [InlineData(75, 100, 75)]      // 75%
    public void Create_ScorePercentage_CalculatesCorrectly(decimal score, decimal maxScore, decimal expectedPercentage)
    {
        // Act
        var submission = UserSubmission.Create(_userId, _quizLectureId, _submittedAtUtc, score, maxScore);

        // Assert
        var actualPercentage = maxScore > 0 ? (submission.Score / submission.MaxScore) * 100 : 0;
        Assert.Equal(expectedPercentage, actualPercentage);
    }

    #endregion
}
