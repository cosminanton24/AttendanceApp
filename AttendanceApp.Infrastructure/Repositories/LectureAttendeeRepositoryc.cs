using AttendanceApp.Domain.Lectures;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Persistence;

namespace AttendanceApp.Infrastructure.Repositories;

public class LectureAttendeeRepository(AttendanceAppDbContext db) : GenericRepository<LectureAttendee>(db), ILectureAttendeeRepository
{
    
}