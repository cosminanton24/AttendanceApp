using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApp.Infrastructure.Repositories;

public class LectureRepository(AttendanceAppDbContext db) : GenericRepository<Lecture>(db), ILectureRepository
{
    public async Task<IReadOnlyList<Lecture>> GetProfessorLecturesAsync(Guid professorId, int pageNumber, int pageSize, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(lecture => lecture.ProfessorId == professorId);

        if (fromDate.HasValue)
        {
            query = query.Where(lecture => lecture.StartTime >= fromDate.Value);
        }

        var lectures = query
            .Include("_attendees")
            .OrderByDescending(lecture => lecture.StartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return await lectures.ToListAsync(cancellationToken);
    }
}