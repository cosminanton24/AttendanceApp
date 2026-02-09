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
    public Location? Location { get; private set; }

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
        Location = null;
    }

    public void Start(Location location)
    {
        Guard.NotNull(location, nameof(location));

         if (Status != LectureStatus.Scheduled)
            throw new DomainException($"Lecture {Status} can only be started when scheduled.");

        Status = LectureStatus.InProgress;
        Location = location;
    }

    public void End()
    {
        if (Status != LectureStatus.InProgress)
            throw new DomainException("Lecture can only be ended when in progress.");

        Status = LectureStatus.Ended;
    }

    public void ChangeStatus(LectureStatus status)
    {
        Status = status;
    }
}
