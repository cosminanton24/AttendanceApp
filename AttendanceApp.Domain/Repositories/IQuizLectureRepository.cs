using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.Domain.Repositories;

public interface IQuizLectureRepository : IRepository<QuizLecture>
{
    Task<QuizLecture?> GetActiveQuizForLectureAsync(Guid lectureId, DateTime utcNow, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuizLecture>> GetQuizLecturesByLectureIdAsync(Guid lectureId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuizLecture>> GetQuizLecturesByQuizIdAsync(Guid quizId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveQuizForLectureAsync(Guid lectureId, DateTime utcNow, CancellationToken cancellationToken = default);
}
