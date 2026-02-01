using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Quizzes;

public sealed class QuizLecture : Entity<Guid>
{
    public Guid LectureId { get; private set; }
    public Guid QuizId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime EndTimeUtc { get; private set; }

    private QuizLecture() { }

    private QuizLecture(
        Guid id,
        Guid lectureId,
        Guid quizId,
        DateTime createdAtUtc,
        TimeSpan quizDuration)
        : base(id)
    {
        Guard.NotEmpty(lectureId, nameof(lectureId));
        Guard.NotEmpty(quizId, nameof(quizId));

        LectureId = lectureId;
        QuizId = quizId;
        CreatedAtUtc = createdAtUtc;
        EndTimeUtc = createdAtUtc.Add(quizDuration);
    }

    public static QuizLecture Create(
        Guid lectureId,
        Guid quizId,
        DateTime createdAtUtc,
        TimeSpan quizDuration)
    {
        return new QuizLecture(
            id: Guid.NewGuid(),
            lectureId: lectureId,
            quizId: quizId,
            createdAtUtc: createdAtUtc,
            quizDuration: quizDuration);
    }

    public bool IsActive(DateTime utcNow) => utcNow < EndTimeUtc;
}
