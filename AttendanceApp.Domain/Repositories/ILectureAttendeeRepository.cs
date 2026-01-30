using AttendanceApp.Domain.Lectures;

namespace AttendanceApp.Domain.Repositories;

public interface ILectureAttendeeRepository : IRepository<LectureAttendee>
{
    Task<LectureAttendee?> GetAttendeeAsync(Guid lectureId, Guid userId, CancellationToken cancellationToken = default);

    Task<int> GetTotalAttendeesAsync(Guid lectureId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LectureAttendee>> GetLectureAttendeesAsync(
        Guid lectureId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}