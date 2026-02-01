using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class QuizRepository(AttendanceAppDbContext db) : GenericRepository<Quiz>(db), IQuizRepository
{
    private readonly AttendanceAppDbContext _context = db;

    public async Task<Quiz?> GetByIdWithQuestionsAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Questions.OrderBy(qu => qu.Order))
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);
    }

    public async Task<Quiz?> GetByIdWithQuestionsAndOptionsAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Questions.OrderBy(qu => qu.Order))
                .ThenInclude(qu => qu.Options.OrderBy(o => o.Order))
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);
    }

    public async Task<IReadOnlyList<Quiz>> GetQuizzesByTeacherAsync(
        Guid teacherId,
        int pageNumber,
        int pageSize,
        string? nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 0);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbSet
            .AsNoTracking()
            .Include(q => q.Questions)
            .Where(q => q.ProfessorId == teacherId);

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(q => q.Name.Contains(nameFilter));
        }

        return await query
            .OrderByDescending(q => q.CreatedAtUtc)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalQuizzesByTeacherAsync(
        Guid teacherId,
        string? nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(q => q.ProfessorId == teacherId);

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            query = query.Where(q => q.Name.Contains(nameFilter));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddQuestionToQuizAsync(Guid quizId, QuizQuestion question, CancellationToken cancellationToken = default)
    {
        _ = await _dbSet
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken)
            ?? throw new KeyNotFoundException($"Quiz {quizId} not found.");

        _context.Set<QuizQuestion>().Add(question);
    }

    public async Task RemoveQuestionFromQuizAsync(Guid quizId, Guid questionId, CancellationToken cancellationToken = default)
    {
        var question = await _context.Set<QuizQuestion>()
            .FirstOrDefaultAsync(q => q.QuizId == quizId && q.Id == questionId, cancellationToken);

        if (question is not null)
        {
            _context.Set<QuizQuestion>().Remove(question);
        }
    }
}
