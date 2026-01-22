using AttendanceApp.Domain.Enums;

namespace AttendanceApp.Application.Features.Lectures.Dtos;

public sealed record LectureDto
(
    Guid Id,
    Guid ProfessorId,
    string Name,
    string Description,
    LectureStatus Status,
    DateTime StartTime,
    TimeSpan Duration,
    int AttendeesCount
);
