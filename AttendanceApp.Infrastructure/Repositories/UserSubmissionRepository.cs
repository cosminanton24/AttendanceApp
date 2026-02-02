using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Domain.Users;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class UserSubmissionRepository : GenericRepository<UserSubmission>, IUserSubmissionRepository
{
    private readonly AttendanceAppDbContext db;
    public UserSubmissionRepository(AttendanceAppDbContext _db) : base(_db)
    {
        db = _db;
    }
    public async Task<UserSubmission?> GetByUserAndQuizLectureAsync(
        Guid userId,
        Guid quizLectureId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                us => us.UserId == userId && us.QuizLectureId == quizLectureId,
                cancellationToken);
    }

    public async Task<bool> HasUserSubmittedAsync(
        Guid userId,
        Guid quizLectureId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(
                us => us.UserId == userId && us.QuizLectureId == quizLectureId && us.Submitted,
                cancellationToken);
    }

    public async Task<int> GetSubmissionCountAsync(
        Guid quizLectureId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(
                us => us.QuizLectureId == quizLectureId && us.Submitted,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserSubmission>> GetUserSubmissionsForQuizLecturesAsync(
        Guid userId,
        IEnumerable<Guid> quizLectureIds,
        CancellationToken cancellationToken = default)
    {
        var idList = quizLectureIds.ToList();
        return await _dbSet
            .Where(us => us.UserId == userId && idList.Contains(us.QuizLectureId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSubmission>> GetSubmissionsByQuizLecturePagedAsync(
        Guid quizLectureId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(us => us.QuizLectureId == quizLectureId && us.Submitted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(us =>
                db.Users.Any(u => u.Id == us.UserId &&
                    (u.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) || u.Email.Contains(term, StringComparison.CurrentCultureIgnoreCase))));
        }

        return await query
            .OrderByDescending(us => us.SubmittedAtUtc)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetSubmissionsTotalCountAsync(
        Guid quizLectureId,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(us => us.QuizLectureId == quizLectureId && us.Submitted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(us =>
                db.Users.Any(u => u.Id == us.UserId &&
                    (u.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) || u.Email.Contains(term, StringComparison.CurrentCultureIgnoreCase))));
        }

        return await query.CountAsync(cancellationToken);
    }
}
