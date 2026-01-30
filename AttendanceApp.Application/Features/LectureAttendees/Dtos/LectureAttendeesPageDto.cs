namespace AttendanceApp.Application.Features.LectureAttendees.Dtos;

public sealed record LectureAttendeesPageDto(
    IReadOnlyList<LectureAttendeeDto> Items,
    int Total
);
