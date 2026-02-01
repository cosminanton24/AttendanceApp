namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record QuizOptionDto(
    Guid Id,
    string Text,
    int Order,
    bool IsCorrect
);
