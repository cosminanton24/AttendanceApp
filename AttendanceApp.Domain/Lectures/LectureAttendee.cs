using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Lectures;

public sealed class LectureAttendee : Entity<Guid>
{
    public Guid LectureId { get; private set; }
    public Guid UserId { get; private set; }
    
    public DateTime TimeJoined { get; init; }

    private LectureAttendee() { }

    public LectureAttendee(Guid lectureId, Guid userId)
    {
        Guard.NotEmpty(lectureId, nameof(lectureId));
        Guard.NotEmpty(userId, nameof(userId));

        LectureId = lectureId;
        UserId = userId;
        TimeJoined = DateTime.UtcNow;
    }
}
