namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record ActiveQuizDto(
    Guid Id,
    Guid QuizLectureId,
    string Name,
    TimeSpan Duration,
    Guid ProfessorId,
    DateTime CreatedAtUtc,
    DateTime ActivatedAtUtc,
    DateTime EndTimeUtc,
    IReadOnlyList<QuizQuestionDto> Questions
);
