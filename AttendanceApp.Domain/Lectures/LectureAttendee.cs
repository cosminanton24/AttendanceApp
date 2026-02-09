using AttendanceApp.Domain.Common;

namespace AttendanceApp.Domain.Lectures;

public sealed class LectureAttendee : Entity<Guid>
{
    public Guid LectureId { get; private set; }
    public Guid UserId { get; private set; }
    
    public DateTime TimeJoined { get; init; }
    public Location LocationAtJoin { get; private set; }

    private LectureAttendee() { }

    public LectureAttendee(Guid lectureId, Guid userId, Location locationAtJoin)
    {
        Guard.NotEmpty(lectureId, nameof(lectureId));
        Guard.NotEmpty(userId, nameof(userId));

        Id = Guid.NewGuid();
        LectureId = lectureId;
        UserId = userId;
        TimeJoined = DateTime.UtcNow;
        LocationAtJoin = locationAtJoin;
    }
}
