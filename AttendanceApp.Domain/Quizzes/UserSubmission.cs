using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Quizzes;

public sealed class UserSubmission : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid QuizLectureId { get; private set; }
    public bool Submitted { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public decimal Score { get; private set; }
    public decimal MaxScore { get; private set; }

    private UserSubmission() { }

    private UserSubmission(
        Guid id,
        Guid userId,
        Guid quizLectureId,
        bool submitted,
        DateTime submittedAtUtc,
        decimal score,
        decimal maxScore)
        : base(id)
    {
        Guard.NotEmpty(userId, nameof(userId));
        Guard.NotEmpty(quizLectureId, nameof(quizLectureId));

        UserId = userId;
        QuizLectureId = quizLectureId;
        Submitted = submitted;
        SubmittedAtUtc = submittedAtUtc;
        Score = score;
        MaxScore = maxScore;
    }

    public static UserSubmission Create(
        Guid userId,
        Guid quizLectureId,
        DateTime submittedAtUtc,
        decimal score,
        decimal maxScore)
    {
        return new UserSubmission(
            id: Guid.NewGuid(),
            userId: userId,
            quizLectureId: quizLectureId,
            submitted: true,
            submittedAtUtc: submittedAtUtc,
            score: score,
            maxScore: maxScore);
    }
}
