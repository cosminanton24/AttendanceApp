using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class QuizOptionRepository(AttendanceAppDbContext db) : GenericRepository<QuizOption>(db), IQuizOptionRepository
{
    public async Task<IReadOnlyList<QuizOption>> GetOptionsByQuestionIdAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.QuestionId == questionId)
            .OrderBy(o => o.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<QuizOption?> GetCorrectOptionAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.QuestionId == questionId && o.IsCorrect, cancellationToken);
    }

    public async Task<IReadOnlyList<QuizOption>> GetCorrectOptionsAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.QuestionId == questionId && o.IsCorrect)
            .OrderBy(o => o.Order)
            .ToListAsync(cancellationToken);
    }
}
