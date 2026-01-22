using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;

namespace AttendanceApp.Infrastructure.Repositories;

public class LectureRepository(AttendanceAppDbContext db) : GenericRepository<Lecture>(db), ILectureRepository
{
    
}