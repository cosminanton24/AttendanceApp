namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record QuizLectureDto(
    Guid Id,
    Guid LectureId,
    Guid QuizId,
    DateTime CreatedAtUtc,
    DateTime EndTimeUtc,
    bool IsActive
);
