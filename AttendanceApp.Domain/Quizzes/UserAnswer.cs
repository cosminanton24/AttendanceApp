using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Quizzes;

public sealed class UserAnswer : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid QuizLectureId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid OptionId { get; private set; }
    public bool Choice { get; private set; }

    private UserAnswer() { }

    private UserAnswer(
        Guid id,
        Guid userId,
        Guid quizLectureId,
        Guid questionId,
        Guid optionId,
        bool choice)
        : base(id)
    {
        Guard.NotEmpty(userId, nameof(userId));
        Guard.NotEmpty(quizLectureId, nameof(quizLectureId));
        Guard.NotEmpty(questionId, nameof(questionId));
        Guard.NotEmpty(optionId, nameof(optionId));

        UserId = userId;
        QuizLectureId = quizLectureId;
        QuestionId = questionId;
        OptionId = optionId;
        Choice = choice;
    }

    public static UserAnswer Create(
        Guid userId,
        Guid quizLectureId,
        Guid questionId,
        Guid optionId,
        bool choice)
    {
        return new UserAnswer(
            id: Guid.NewGuid(),
            userId: userId,
            quizLectureId: quizLectureId,
            questionId: questionId,
            optionId: optionId,
            choice: choice);
    }

    public void UpdateChoice(bool newChoice)
    {
        Choice = newChoice;
    }
}
