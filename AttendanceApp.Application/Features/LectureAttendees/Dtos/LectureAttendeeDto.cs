namespace AttendanceApp.Application.Features.LectureAttendees.Dtos;

public sealed record LectureAttendeeDto(
    Guid UserId,
    DateTime TimeJoined
);
