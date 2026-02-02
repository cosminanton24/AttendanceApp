using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.UnitTests.Domain;

public class QuizTests
{
    private readonly Guid _professorId = Guid.NewGuid();
    private readonly string _name = "Final Exam";
    private readonly TimeSpan _duration = TimeSpan.FromMinutes(60);
    private readonly DateTime _createdAtUtc = DateTime.UtcNow;

    #region Create Tests

    [Fact]
    public void Create_WithValidData_CreatesQuiz()
    {
        // Act
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);

        // Assert
        Assert.NotEqual(Guid.Empty, quiz.Id);
        Assert.Equal(_name, quiz.Name);
        Assert.Equal(_duration, quiz.Duration);
        Assert.Equal(_professorId, quiz.ProfessorId);
        Assert.Equal(_createdAtUtc, quiz.CreatedAtUtc);
        Assert.Empty(quiz.Questions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ThrowsDomainException(string? invalidName)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            Quiz.Create(invalidName!, _duration, _professorId, _createdAtUtc));
    }

    [Fact]
    public void Create_WithZeroDuration_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            Quiz.Create(_name, TimeSpan.Zero, _professorId, _createdAtUtc));
    }

    [Fact]
    public void Create_WithNegativeDuration_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            Quiz.Create(_name, TimeSpan.FromMinutes(-10), _professorId, _createdAtUtc));
    }

    [Fact]
    public void Create_WithEmptyProfessorId_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            Quiz.Create(_name, _duration, Guid.Empty, _createdAtUtc));
    }

    [Fact]
    public void Create_TrimsName()
    {
        // Arrange
        var nameWithSpaces = "  Test Quiz  ";

        // Act
        var quiz = Quiz.Create(nameWithSpaces, _duration, _professorId, _createdAtUtc);

        // Assert
        Assert.Equal("Test Quiz", quiz.Name);
    }

    #endregion

    #region Rename Tests

    [Fact]
    public void Rename_WithValidName_ChangesName()
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);
        var newName = "Midterm Exam";

        // Act
        quiz.Rename(newName);

        // Assert
        Assert.Equal(newName, quiz.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithInvalidName_ThrowsDomainException(string? invalidName)
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);

        // Act & Assert
        Assert.Throws<DomainException>(() => quiz.Rename(invalidName!));
    }

    [Fact]
    public void Rename_TrimsNewName()
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);

        // Act
        quiz.Rename("  New Name  ");

        // Assert
        Assert.Equal("New Name", quiz.Name);
    }

    #endregion

    #region ChangeDuration Tests

    [Fact]
    public void ChangeDuration_WithValidDuration_ChangesDuration()
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);
        var newDuration = TimeSpan.FromMinutes(90);

        // Act
        quiz.ChangeDuration(newDuration);

        // Assert
        Assert.Equal(newDuration, quiz.Duration);
    }

    [Fact]
    public void ChangeDuration_WithZeroDuration_ThrowsDomainException()
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);

        // Act & Assert
        Assert.Throws<DomainException>(() => quiz.ChangeDuration(TimeSpan.Zero));
    }

    [Fact]
    public void ChangeDuration_WithNegativeDuration_ThrowsDomainException()
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);

        // Act & Assert
        Assert.Throws<DomainException>(() => quiz.ChangeDuration(TimeSpan.FromMinutes(-5)));
    }

    #endregion

    #region AddQuestion Tests

    [Fact]
    public void AddQuestion_WithValidData_AddsQuestion()
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);

        // Act
        var question = quiz.AddQuestion("What is 2+2?", 1, 5);

        // Assert
        Assert.Single(quiz.Questions);
        Assert.Equal(quiz.Id, question.QuizId);
        Assert.Equal("What is 2+2?", question.Text);
        Assert.Equal(1, question.Order);
        Assert.Equal(5m, question.Points);
    }

    [Fact]
    public void AddQuestion_MultipleQuestions_AddsAll()
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);

        // Act
        quiz.AddQuestion("Question 1", 1);
        quiz.AddQuestion("Question 2", 2);
        quiz.AddQuestion("Question 3", 3);

        // Assert
        Assert.Equal(3, quiz.Questions.Count);
    }

    [Fact]
    public void AddQuestion_WithNullPoints_CreatesQuestionWithNullPoints()
    {
        // Arrange
        var quiz = Quiz.Create(_name, _duration, _professorId, _createdAtUtc);

        // Act
        var question = quiz.AddQuestion("Question text", 1, null);

        // Assert
        Assert.Null(question.Points);
    }

    #endregion
}
