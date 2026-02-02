using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.UnitTests.Domain;

public class QuizQuestionTests
{
    private readonly Guid _quizId = Guid.NewGuid();
    private readonly string _text = "What is the capital of France?";
    private readonly int _order = 1;
    private readonly decimal? _points = 10m;

    #region Create Tests (via Quiz.AddQuestion)

    [Fact]
    public void AddQuestion_WithValidData_CreatesQuestion()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);

        // Act
        var question = quiz.AddQuestion(_text, _order, _points);

        // Assert
        Assert.NotEqual(Guid.Empty, question.Id);
        Assert.Equal(quiz.Id, question.QuizId);
        Assert.Equal(_text, question.Text);
        Assert.Equal(_order, question.Order);
        Assert.Equal(_points, question.Points);
        Assert.Empty(question.Options);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddQuestion_WithInvalidText_ThrowsDomainException(string? invalidText)
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<DomainException>(() => quiz.AddQuestion(invalidText!, _order, _points));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AddQuestion_WithInvalidOrder_ThrowsDomainException(int invalidOrder)
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<DomainException>(() => quiz.AddQuestion(_text, invalidOrder, _points));
    }

    [Fact]
    public void AddQuestion_WithNegativePoints_ThrowsDomainException()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<DomainException>(() => quiz.AddQuestion(_text, _order, -5m));
    }

    [Fact]
    public void AddQuestion_TrimsText()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);

        // Act
        var question = quiz.AddQuestion("  Trimmed Question  ", _order, _points);

        // Assert
        Assert.Equal("Trimmed Question", question.Text);
    }

    #endregion

    #region ChangeText Tests

    [Fact]
    public void ChangeText_WithValidText_ChangesText()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);
        var newText = "Updated question text";

        // Act
        question.ChangeText(newText);

        // Assert
        Assert.Equal(newText, question.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeText_WithInvalidText_ThrowsDomainException(string? invalidText)
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act & Assert
        Assert.Throws<DomainException>(() => question.ChangeText(invalidText!));
    }

    [Fact]
    public void ChangeText_TrimsNewText()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act
        question.ChangeText("  New Text  ");

        // Assert
        Assert.Equal("New Text", question.Text);
    }

    #endregion

    #region ChangeOrder Tests

    [Fact]
    public void ChangeOrder_WithValidOrder_ChangesOrder()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act
        question.ChangeOrder(5);

        // Assert
        Assert.Equal(5, question.Order);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangeOrder_WithInvalidOrder_ThrowsDomainException(int invalidOrder)
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act & Assert
        Assert.Throws<DomainException>(() => question.ChangeOrder(invalidOrder));
    }

    #endregion

    #region ChangePoints Tests

    [Fact]
    public void ChangePoints_WithValidPoints_ChangesPoints()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act
        question.ChangePoints(20m);

        // Assert
        Assert.Equal(20m, question.Points);
    }

    [Fact]
    public void ChangePoints_WithNull_SetsPointsToNull()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, 10m);

        // Act
        question.ChangePoints(null);

        // Assert
        Assert.Null(question.Points);
    }

    [Fact]
    public void ChangePoints_WithNegativePoints_ThrowsDomainException()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act & Assert
        Assert.Throws<DomainException>(() => question.ChangePoints(-5m));
    }

    [Fact]
    public void ChangePoints_WithZero_IsValid()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act
        question.ChangePoints(0m);

        // Assert
        Assert.Equal(0m, question.Points);
    }

    #endregion

    #region AddOption Tests

    [Fact]
    public void AddOption_WithValidData_AddsOption()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act
        var option = question.AddOption("Paris", 1, true);

        // Assert
        Assert.Single(question.Options);
        Assert.Equal(question.Id, option.QuestionId);
        Assert.Equal("Paris", option.Text);
        Assert.Equal(1, option.Order);
        Assert.True(option.IsCorrect);
    }

    [Fact]
    public void AddOption_MultipleOptions_AddsAll()
    {
        // Arrange
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        var question = quiz.AddQuestion(_text, _order, _points);

        // Act
        question.AddOption("Paris", 1, true);
        question.AddOption("London", 2, false);
        question.AddOption("Berlin", 3, false);

        // Assert
        Assert.Equal(3, question.Options.Count);
    }

    #endregion
}
