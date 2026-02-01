using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Quizzes;

public sealed class Quiz : AggregateRoot<Guid>
{
    private readonly List<QuizQuestion> _questions = [];

    public string Name { get; private set; } = default!;
    public TimeSpan Duration { get; private set; }

    public Guid ProfessorId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<QuizQuestion> Questions => _questions.AsReadOnly();

    private Quiz() { }

    private Quiz(
        Guid id,
        string name,
        TimeSpan duration,
        Guid createdByTeacherId,
        DateTime createdAtUtc)
        : base(id)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.Positive(duration, nameof(duration));
        Guard.NotEmpty(createdByTeacherId, nameof(createdByTeacherId));

        Name = name.Trim();
        Duration = duration;
        ProfessorId = createdByTeacherId;
        CreatedAtUtc = createdAtUtc;
    }

    public static Quiz Create(
        string name,
        TimeSpan duration,
        Guid createdByTeacherId,
        DateTime createdAtUtc)
    {
        Guard.NotNullOrWhiteSpace(name, nameof(name));
        Guard.Positive(duration, nameof(duration));
        Guard.NotEmpty(createdByTeacherId, nameof(createdByTeacherId));

        return new Quiz(
            id: Guid.NewGuid(),
            name: name,
            duration: duration,
            createdByTeacherId: createdByTeacherId,
            createdAtUtc: createdAtUtc);
    }

    // Behavior

    public void Rename(string newName)
    {
        Guard.NotNullOrWhiteSpace(newName, nameof(newName));
        Name = newName.Trim();
    }

    public void ChangeDuration(TimeSpan newDuration)
    {
        Guard.Positive(newDuration, nameof(newDuration));
        Duration = newDuration;
    }

    public QuizQuestion AddQuestion(string text, int order, decimal? points = null)
    {
        var question = QuizQuestion.Create(
            quizId: Id,
            text: text,
            order: order,
            points: points);

        _questions.Add(question);
        return question;
    }

    public void RemoveQuestion(Guid questionId)
    {
        var question = _questions.SingleOrDefault(q => q.Id == questionId);
        if (question is null) return;

        _questions.Remove(question);
    }
}