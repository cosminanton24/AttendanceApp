using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.Domain.Repositories;

public interface IQuizOptionRepository : IRepository<QuizOption>
{
    Task<IReadOnlyList<QuizOption>> GetOptionsByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken = default);

    Task<QuizOption?> GetCorrectOptionAsync(Guid questionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuizOption>> GetCorrectOptionsAsync(Guid questionId, CancellationToken cancellationToken = default);
}
