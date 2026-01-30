using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class LectureAttendeeRepository(AttendanceAppDbContext db) : GenericRepository<LectureAttendee>(db), ILectureAttendeeRepository
{
    public async Task<LectureAttendee?> GetAttendeeAsync(Guid lectureId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(la => la.LectureId == lectureId && la.UserId == userId, cancellationToken);
    }

    public async Task<int> GetTotalAttendeesAsync(Guid lectureId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().CountAsync(x => x.LectureId == lectureId, cancellationToken);
    }

    public async Task<IReadOnlyList<LectureAttendee>> GetLectureAttendeesAsync(
        Guid lectureId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 0) page = 0;
        if (pageSize <= 0) pageSize = 20;

        return await _dbSet
            .AsNoTracking()
            .Where(x => x.LectureId == lectureId)
            .OrderBy(x => x.TimeJoined)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    
}