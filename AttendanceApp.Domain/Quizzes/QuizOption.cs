using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Quizzes;

public sealed class QuizOption : Entity<Guid>
{
    public Guid QuestionId { get; private set; }

    public string Text { get; private set; } = default!;
    public int Order { get; private set; }
    public bool IsCorrect { get; private set; }

    private QuizOption() { }

    private QuizOption(
        Guid id,
        Guid questionId,
        string text,
        int order,
        bool isCorrect)
        : base(id)
    {
        Guard.NotEmpty(questionId, nameof(questionId));
        Guard.NotNullOrWhiteSpace(text, nameof(text));
        Guard.InRange(order, nameof(order), 1, int.MaxValue);

        QuestionId = questionId;
        Text = text.Trim();
        Order = order;
        IsCorrect = isCorrect;
    }

    internal static QuizOption Create(
        Guid questionId,
        string text,
        int order,
        bool isCorrect)
    {
        return new QuizOption(
            id: Guid.NewGuid(),
            questionId: questionId,
            text: text,
            order: order,
            isCorrect: isCorrect);
    }

    public void ChangeText(string newText)
    {
        Guard.NotNullOrWhiteSpace(newText, nameof(newText));
        Text = newText.Trim();
    }

    public void ChangeOrder(int newOrder)
    {
        Guard.InRange(newOrder, nameof(newOrder), 1, int.MaxValue);
        Order = newOrder;
    }

    public void MarkAsCorrect() => IsCorrect = true;
    public void MarkAsIncorrect() => IsCorrect = false;
}