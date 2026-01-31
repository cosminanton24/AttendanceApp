using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class LectureRepository(AttendanceAppDbContext db) : GenericRepository<Lecture>(db), ILectureRepository
{
    private readonly AttendanceAppDbContext db = db;

    public async Task<IReadOnlyList<Lecture>> GetProfessorLecturesAsync(Guid professorId, int pageNumber, int pageSize, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 0);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        var query = _dbSet
            .Where(lecture => lecture.ProfessorId == professorId);

        if (fromDate.HasValue)
        {
            query = query.Where(lecture => lecture.StartTime.Month == fromDate.Value.Month && lecture.StartTime.Year == fromDate.Value.Year);
        }

        var lectures = query
            .Include(l => l.Attendees)
            .OrderByDescending(lecture => lecture.StartTime)
            .Skip(pageNumber * pageSize)
            .Take(pageSize);

        return await lectures.ToListAsync(cancellationToken);
    }

    public async Task<bool> HasProfessorConflictingLectureAsync(Guid professorId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        var potentialConflicts = await _dbSet
            .Where(lecture =>
                lecture.ProfessorId == professorId &&
                lecture.StartTime < endTime &&
                lecture.StartTime >= startTime.AddDays(-1))
            .ToListAsync(cancellationToken);

        return potentialConflicts.Any(lecture =>
        {
            var lectureEnd = lecture.StartTime.Add(lecture.Duration);
            return startTime < lectureEnd && endTime > lecture.StartTime;
        });
    }

    public async Task<IReadOnlyList<Lecture>> GetStudentLecturesAsync(
        Guid userId, 
        int pageNumber, 
        int pageSize, 
        DateTime? fromDate = null,
        LectureStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 0);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Lectures
            .AsNoTracking()
            .Include(l => l.Attendees)
            .Where(l =>
                db.Set<LectureAttendee>()
                    .Any(a => a.UserId == userId && a.LectureId == l.Id));

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }
        if (fromDate.HasValue)
        {
            query = query.Where(lecture => lecture.StartTime >= fromDate.Value);
        }

        return await query
            .OrderByDescending(l => l.StartTime)
            .ThenBy(l => l.Id)
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}