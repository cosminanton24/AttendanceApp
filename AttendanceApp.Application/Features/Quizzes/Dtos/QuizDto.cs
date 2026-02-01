namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record QuizDto(
    Guid Id,
    string Name,
    TimeSpan Duration,
    Guid ProfessorId,
    DateTime CreatedAtUtc,
    int QuestionsCount
);
