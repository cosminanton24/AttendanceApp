using AttendanceApp.Domain.Quizzes;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class UserAnswerRepository(AttendanceAppDbContext db) : GenericRepository<UserAnswer>(db), IUserAnswerRepository
{
    public async Task<UserAnswer?> GetByUserQuizLectureQuestionOptionAsync(
        Guid userId,
        Guid quizLectureId,
        Guid questionId,
        Guid optionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                ua => ua.UserId == userId
                    && ua.QuizLectureId == quizLectureId
                    && ua.QuestionId == questionId
                    && ua.OptionId == optionId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserAnswer>> GetByUserAndQuizLectureAsync(
        Guid userId,
        Guid quizLectureId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.QuizLectureId == quizLectureId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasUserSubmittedAsync(
        Guid userId,
        Guid quizLectureId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(ua => ua.UserId == userId && ua.QuizLectureId == quizLectureId, cancellationToken);
    }
}
