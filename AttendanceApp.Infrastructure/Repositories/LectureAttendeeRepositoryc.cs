using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;

namespace AttendanceApp.Infrastructure.Repositories;

public class LectureAttendeeRepository(AttendanceAppAppDbContext db) : GenericRepository<LectureAttendee>(db), ILectureAttendeeRepository
{
    
}