using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.Domain.Repositories;

public interface IQuizRepository : IRepository<Quiz>
{
    Task<Quiz?> GetByIdWithQuestionsAsync(Guid quizId, CancellationToken cancellationToken = default);

    Task<Quiz?> GetByIdWithQuestionsAndOptionsAsync(Guid quizId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Quiz>> GetQuizzesByTeacherAsync(
        Guid teacherId,
        int pageNumber,
        int pageSize,
        string? nameFilter = null,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalQuizzesByTeacherAsync(
        Guid teacherId,
        string? nameFilter = null,
        CancellationToken cancellationToken = default);

    Task AddQuestionToQuizAsync(Guid quizId, QuizQuestion question, CancellationToken cancellationToken = default);

    Task RemoveQuestionFromQuizAsync(Guid quizId, Guid questionId, CancellationToken cancellationToken = default);
}
