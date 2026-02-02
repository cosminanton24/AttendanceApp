using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.UnitTests.Domain;

public class QuizOptionTests
{
    private readonly string _text = "Option A";
    private readonly int _order = 1;
    private readonly bool _isCorrect = false;

    private static QuizQuestion CreateQuestion()
    {
        var quiz = Quiz.Create("Test Quiz", TimeSpan.FromMinutes(30), Guid.NewGuid(), DateTime.UtcNow);
        return quiz.AddQuestion("Test Question", 1, 5m);
    }

    #region Create Tests (via QuizQuestion.AddOption)

    [Fact]
    public void AddOption_WithValidData_CreatesOption()
    {
        // Arrange
        var question = CreateQuestion();

        // Act
        var option = question.AddOption(_text, _order, _isCorrect);

        // Assert
        Assert.NotEqual(Guid.Empty, option.Id);
        Assert.Equal(question.Id, option.QuestionId);
        Assert.Equal(_text, option.Text);
        Assert.Equal(_order, option.Order);
        Assert.Equal(_isCorrect, option.IsCorrect);
    }

    [Fact]
    public void AddOption_WithCorrectTrue_SetsIsCorrectToTrue()
    {
        // Arrange
        var question = CreateQuestion();

        // Act
        var option = question.AddOption("Correct Answer", 1, true);

        // Assert
        Assert.True(option.IsCorrect);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddOption_WithInvalidText_ThrowsDomainException(string? invalidText)
    {
        // Arrange
        var question = CreateQuestion();

        // Act & Assert
        Assert.Throws<DomainException>(() => question.AddOption(invalidText!, _order, _isCorrect));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AddOption_WithInvalidOrder_ThrowsDomainException(int invalidOrder)
    {
        // Arrange
        var question = CreateQuestion();

        // Act & Assert
        Assert.Throws<DomainException>(() => question.AddOption(_text, invalidOrder, _isCorrect));
    }

    [Fact]
    public void AddOption_TrimsText()
    {
        // Arrange
        var question = CreateQuestion();

        // Act
        var option = question.AddOption("  Trimmed Option  ", _order, _isCorrect);

        // Assert
        Assert.Equal("Trimmed Option", option.Text);
    }

    #endregion

    #region ChangeText Tests

    [Fact]
    public void ChangeText_WithValidText_ChangesText()
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, _isCorrect);
        var newText = "Updated Option";

        // Act
        option.ChangeText(newText);

        // Assert
        Assert.Equal(newText, option.Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeText_WithInvalidText_ThrowsDomainException(string? invalidText)
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, _isCorrect);

        // Act & Assert
        Assert.Throws<DomainException>(() => option.ChangeText(invalidText!));
    }

    [Fact]
    public void ChangeText_TrimsNewText()
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, _isCorrect);

        // Act
        option.ChangeText("  New Text  ");

        // Assert
        Assert.Equal("New Text", option.Text);
    }

    #endregion

    #region ChangeOrder Tests

    [Fact]
    public void ChangeOrder_WithValidOrder_ChangesOrder()
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, _isCorrect);

        // Act
        option.ChangeOrder(5);

        // Assert
        Assert.Equal(5, option.Order);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangeOrder_WithInvalidOrder_ThrowsDomainException(int invalidOrder)
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, _isCorrect);

        // Act & Assert
        Assert.Throws<DomainException>(() => option.ChangeOrder(invalidOrder));
    }

    #endregion

    #region MarkAsCorrect / MarkAsIncorrect Tests

    [Fact]
    public void MarkAsCorrect_SetsIsCorrectToTrue()
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, false);
        Assert.False(option.IsCorrect);

        // Act
        option.MarkAsCorrect();

        // Assert
        Assert.True(option.IsCorrect);
    }

    [Fact]
    public void MarkAsIncorrect_SetsIsCorrectToFalse()
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, true);
        Assert.True(option.IsCorrect);

        // Act
        option.MarkAsIncorrect();

        // Assert
        Assert.False(option.IsCorrect);
    }

    [Fact]
    public void MarkAsCorrect_AlreadyCorrect_RemainsCorrect()
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, true);

        // Act
        option.MarkAsCorrect();

        // Assert
        Assert.True(option.IsCorrect);
    }

    [Fact]
    public void MarkAsIncorrect_AlreadyIncorrect_RemainsIncorrect()
    {
        // Arrange
        var question = CreateQuestion();
        var option = question.AddOption(_text, _order, false);

        // Act
        option.MarkAsIncorrect();

        // Assert
        Assert.False(option.IsCorrect);
    }

    #endregion
}
