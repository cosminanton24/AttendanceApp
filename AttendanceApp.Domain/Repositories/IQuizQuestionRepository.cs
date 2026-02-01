using AttendanceApp.Domain.Quizzes;

namespace AttendanceApp.Domain.Repositories;

public interface IQuizQuestionRepository : IRepository<QuizQuestion>
{
    Task<QuizQuestion?> GetByIdWithOptionsAsync(Guid questionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QuizQuestion>> GetQuestionsByQuizIdAsync(Guid quizId, CancellationToken cancellationToken = default);

    Task AddOptionToQuestionAsync(Guid questionId, QuizOption option, CancellationToken cancellationToken = default);

    Task RemoveOptionFromQuestionAsync(Guid questionId, Guid optionId, CancellationToken cancellationToken = default);
}
