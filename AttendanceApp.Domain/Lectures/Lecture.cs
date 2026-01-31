using System.ComponentModel.DataAnnotations.Schema;
using AttendanceApp.Domain.Common;
using AttendanceApp.Domain.Enums;

namespace AttendanceApp.Domain.Lectures;

public class Lecture : AggregateRoot<Guid>
{
    private readonly List<LectureAttendee> _attendees = [];

    public Guid ProfessorId { get; }
    public LectureStatus Status { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public DateTime StartTime { get; }
    public TimeSpan Duration { get; }
    [NotMapped]
    public DateTime EndTime => StartTime.Add(Duration);

    public IReadOnlyCollection<LectureAttendee> Attendees => _attendees.AsReadOnly();

    private Lecture() { }

    public Lecture(Guid id, Guid professorId, string name, string description, DateTime startTime, TimeSpan duration)
    {
        Guard.NotEmpty(id, nameof(id));
        Guard.NotEmpty(professorId, nameof(professorId));
        Guard.NotInPast(startTime, nameof(startTime));
        Guard.Positive(duration, nameof(duration));

        Id = id;
        ProfessorId = professorId;
        StartTime = startTime;
        Duration = duration;
        Name = name;
        Description = description;
        Status = LectureStatus.Scheduled;
    }

    public void Start()
    {
        if (Status != LectureStatus.Scheduled)
            throw new DomainException("Lecture can only be started when scheduled.");

        Status = LectureStatus.InProgress;
    }

    public void End()
    {
        if (Status != LectureStatus.InProgress)
            throw new DomainException("Lecture can only be ended when in progress.");

        Status = LectureStatus.Ended;
    }

    public void AddAttendee(Guid userId)
    {
        Guard.NotEmpty(userId, nameof(userId));

        if (Status != LectureStatus.InProgress)
            throw new DomainException("Cannot add attendees unless lecture is in progress.");

        if (_attendees.Any(a => a.UserId == userId))
            return;

        _attendees.Add(new LectureAttendee(Id, userId));
    }

    public void ChangeStatus(LectureStatus status)
    {
        Status = status;
    }
}
