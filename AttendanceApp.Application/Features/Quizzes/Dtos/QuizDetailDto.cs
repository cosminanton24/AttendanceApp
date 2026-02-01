namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record QuizDetailDto(
    Guid Id,
    string Name,
    TimeSpan Duration,
    Guid ProfessorId,
    DateTime CreatedAtUtc,
    IReadOnlyList<QuizQuestionDto> Questions
);
