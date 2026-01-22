using AttendanceApp.Domain.Lectures;

namespace AttendanceApp.Domain.Repositories;

public interface ILectureAttendeeRepository : IRepository<LectureAttendee>
{
    Task<LectureAttendee?> GetAttendeeAsync(Guid lectureId, Guid userId, CancellationToken cancellationToken = default);
}