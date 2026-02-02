using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.Domain.Repositories;

public interface IUserAnswerRepository : IRepository<UserAnswer>
{
    Task<UserAnswer?> GetByUserQuizLectureQuestionOptionAsync(
        Guid userId,
        Guid quizLectureId,
        Guid questionId,
        Guid optionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAnswer>> GetByUserAndQuizLectureAsync(
        Guid userId,
        Guid quizLectureId,
        CancellationToken cancellationToken = default);

    Task<bool> HasUserSubmittedAsync(
        Guid userId,
        Guid quizLectureId,
        CancellationToken cancellationToken = default);
}
