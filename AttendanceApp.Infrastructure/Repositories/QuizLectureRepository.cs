using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class QuizLectureRepository(AttendanceAppDbContext db) : GenericRepository<QuizLecture>(db), IQuizLectureRepository
{
    public async Task<QuizLecture?> GetActiveQuizForLectureAsync(Guid lectureId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ql => ql.LectureId == lectureId && ql.EndTimeUtc > utcNow, cancellationToken);
    }

    public async Task<IReadOnlyList<QuizLecture>> GetQuizLecturesByLectureIdAsync(Guid lectureId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ql => ql.LectureId == lectureId)
            .OrderByDescending(ql => ql.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuizLecture>> GetQuizLecturesByQuizIdAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ql => ql.QuizId == quizId)
            .OrderByDescending(ql => ql.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveQuizForLectureAsync(Guid lectureId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(ql => ql.LectureId == lectureId && ql.EndTimeUtc > utcNow, cancellationToken);
    }
}
