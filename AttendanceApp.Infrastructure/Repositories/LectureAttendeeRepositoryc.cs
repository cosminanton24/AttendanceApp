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
}