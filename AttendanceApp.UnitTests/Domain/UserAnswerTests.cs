using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.UnitTests.Domain;

public class UserAnswerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _quizLectureId = Guid.NewGuid();
    private readonly Guid _questionId = Guid.NewGuid();
    private readonly Guid _optionId = Guid.NewGuid();
    private readonly bool _choice = true;

    #region Create Tests

    [Fact]
    public void Create_WithValidData_CreatesUserAnswer()
    {
        // Act
        var userAnswer = UserAnswer.Create(_userId, _quizLectureId, _questionId, _optionId, _choice);

        // Assert
        Assert.NotEqual(Guid.Empty, userAnswer.Id);
        Assert.Equal(_userId, userAnswer.UserId);
        Assert.Equal(_quizLectureId, userAnswer.QuizLectureId);
        Assert.Equal(_questionId, userAnswer.QuestionId);
        Assert.Equal(_optionId, userAnswer.OptionId);
        Assert.Equal(_choice, userAnswer.Choice);
    }

    [Fact]
    public void Create_WithChoiceTrue_SetsChoiceToTrue()
    {
        // Act
        var userAnswer = UserAnswer.Create(_userId, _quizLectureId, _questionId, _optionId, true);

        // Assert
        Assert.True(userAnswer.Choice);
    }

    [Fact]
    public void Create_WithChoiceFalse_SetsChoiceToFalse()
    {
        // Act
        var userAnswer = UserAnswer.Create(_userId, _quizLectureId, _questionId, _optionId, false);

        // Assert
        Assert.False(userAnswer.Choice);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            UserAnswer.Create(Guid.Empty, _quizLectureId, _questionId, _optionId, _choice));
    }

    [Fact]
    public void Create_WithEmptyQuizLectureId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            UserAnswer.Create(_userId, Guid.Empty, _questionId, _optionId, _choice));
    }

    [Fact]
    public void Create_WithEmptyQuestionId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            UserAnswer.Create(_userId, _quizLectureId, Guid.Empty, _optionId, _choice));
    }

    [Fact]
    public void Create_WithEmptyOptionId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            UserAnswer.Create(_userId, _quizLectureId, _questionId, Guid.Empty, _choice));
    }

    #endregion

    #region UpdateChoice Tests

    [Fact]
    public void UpdateChoice_FromTrueToFalse_UpdatesChoice()
    {
        // Arrange
        var userAnswer = UserAnswer.Create(_userId, _quizLectureId, _questionId, _optionId, true);
        Assert.True(userAnswer.Choice);

        // Act
        userAnswer.UpdateChoice(false);

        // Assert
        Assert.False(userAnswer.Choice);
    }

    [Fact]
    public void UpdateChoice_FromFalseToTrue_UpdatesChoice()
    {
        // Arrange
        var userAnswer = UserAnswer.Create(_userId, _quizLectureId, _questionId, _optionId, false);
        Assert.False(userAnswer.Choice);

        // Act
        userAnswer.UpdateChoice(true);

        // Assert
        Assert.True(userAnswer.Choice);
    }

    [Fact]
    public void UpdateChoice_SameValue_NoChange()
    {
        // Arrange
        var userAnswer = UserAnswer.Create(_userId, _quizLectureId, _questionId, _optionId, true);

        // Act
        userAnswer.UpdateChoice(true);

        // Assert
        Assert.True(userAnswer.Choice);
    }

    [Fact]
    public void UpdateChoice_MultipleUpdates_TracksLastValue()
    {
        // Arrange
        var userAnswer = UserAnswer.Create(_userId, _quizLectureId, _questionId, _optionId, true);

        // Act
        userAnswer.UpdateChoice(false);
        userAnswer.UpdateChoice(true);
        userAnswer.UpdateChoice(false);

        // Assert
        Assert.False(userAnswer.Choice);
    }

    #endregion
}
