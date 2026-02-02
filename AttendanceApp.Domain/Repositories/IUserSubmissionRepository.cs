using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.Domain.Repositories;

public interface IUserSubmissionRepository : IRepository<UserSubmission>
{
    Task<UserSubmission?> GetByUserAndQuizLectureAsync(
        Guid userId,
        Guid quizLectureId,
        CancellationToken cancellationToken = default);

    Task<bool> HasUserSubmittedAsync(
        Guid userId,
        Guid quizLectureId,
        CancellationToken cancellationToken = default);

    Task<int> GetSubmissionCountAsync(
        Guid quizLectureId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSubmission>> GetUserSubmissionsForQuizLecturesAsync(
        Guid userId,
        IEnumerable<Guid> quizLectureIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSubmission>> GetSubmissionsByQuizLecturePagedAsync(
        Guid quizLectureId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<int> GetSubmissionsTotalCountAsync(
        Guid quizLectureId,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
}
