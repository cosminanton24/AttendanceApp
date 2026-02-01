using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class QuizQuestionRepository(AttendanceAppDbContext db) : GenericRepository<QuizQuestion>(db), IQuizQuestionRepository
{
    private readonly AttendanceAppDbContext _context = db;

    public async Task<QuizQuestion?> GetByIdWithOptionsAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Options.OrderBy(o => o.Order))
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken);
    }

    public async Task<IReadOnlyList<QuizQuestion>> GetQuestionsByQuizIdAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(q => q.QuizId == quizId)
            .OrderBy(q => q.Order)
            .Include(q => q.Options.OrderBy(o => o.Order))
            .ToListAsync(cancellationToken);
    }

    public async Task AddOptionToQuestionAsync(Guid questionId, QuizOption option, CancellationToken cancellationToken = default)
    {
        var question = await _dbSet
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Question {questionId} not found.");

        _context.Set<QuizOption>().Add(option);
    }

    public async Task RemoveOptionFromQuestionAsync(Guid questionId, Guid optionId, CancellationToken cancellationToken = default)
    {
        var option = await _context.Set<QuizOption>()
            .FirstOrDefaultAsync(o => o.QuestionId == questionId && o.Id == optionId, cancellationToken);

        if (option is not null)
        {
            _context.Set<QuizOption>().Remove(option);
        }
    }
}
