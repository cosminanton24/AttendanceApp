using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Quizzes;

public sealed class QuizQuestion : Entity<Guid>
{
    private readonly List<QuizOption> _options = [];

    public Guid QuizId { get; private set; }
    public string Text { get; private set; } = default!;
    public int Order { get; private set; }
    public decimal? Points { get; private set; }

    public IReadOnlyCollection<QuizOption> Options => _options.AsReadOnly();

    private QuizQuestion() { }

    private QuizQuestion(
        Guid id,
        Guid quizId,
        string text,
        int order,
        decimal? points)
        : base(id)
    {
        Guard.NotEmpty(quizId, nameof(quizId));
        Guard.NotNullOrWhiteSpace(text, nameof(text));
        Guard.InRange(order, nameof(order), 1, int.MaxValue);
        if (points is not null)
            Guard.NotNegative(points.Value, nameof(points));

        QuizId = quizId;
        Text = text.Trim();
        Order = order;
        Points = points;
    }

    internal static QuizQuestion Create(
        Guid quizId,
        string text,
        int order,
        decimal? points)
    {
        return new QuizQuestion(
            id: Guid.NewGuid(),
            quizId: quizId,
            text: text,
            order: order,
            points: points);
    }

    // Behavior

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

    public void ChangePoints(decimal? points)
    {
        if (points is not null)
            Guard.NotNegative(points.Value, nameof(points));

        Points = points;
    }

    public QuizOption AddOption(string text, int order, bool isCorrect)
    {
        var option = QuizOption.Create(
            questionId: Id,
            text: text,
            order: order,
            isCorrect: isCorrect);

        _options.Add(option);
        return option;
    }

    public void RemoveOption(Guid optionId)
    {
        var option = _options.SingleOrDefault(o => o.Id == optionId);
        if (option is null) return;

        _options.Remove(option);
    }

    public void ClearCorrectOptions()
    {
        foreach (var option in _options)
        {
            option.MarkAsIncorrect();
        }
    }
}