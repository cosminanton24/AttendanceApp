using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class LectureAttendeeRepository(AttendanceAppDbContext db) : GenericRepository<LectureAttendee>(db), ILectureAttendeeRepository
{
    private readonly AttendanceAppDbContext _context = db;

    public async Task<LectureAttendee?> GetAttendeeAsync(Guid lectureId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(la => la.LectureId == lectureId && la.UserId == userId, cancellationToken);
    }

    public async Task<int> GetTotalAttendeesAsync(Guid lectureId, string? searchFilter = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(x => x.LectureId == lectureId);

        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            var filter = searchFilter.Trim().ToLower();
            query = query.Where(a => 
                _context.Users.Any(u => 
                    u.Id == a.UserId && 
                    (u.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) || u.Email.Contains(filter, StringComparison.CurrentCultureIgnoreCase))));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LectureAttendee>> GetLectureAttendeesAsync(
        Guid lectureId,
        int page,
        int pageSize,
        string? searchFilter = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 0) page = 0;
        if (pageSize <= 0) pageSize = 20;

        var query = _dbSet.AsNoTracking().Where(x => x.LectureId == lectureId);

        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            var filter = searchFilter.Trim().ToLower();
            query = query.Where(a => 
                _context.Users.Any(u => 
                    u.Id == a.UserId && 
                    (u.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) || u.Email.Contains(filter, StringComparison.CurrentCultureIgnoreCase))));
        }

        return await query
            .OrderBy(x => x.TimeJoined)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    
}