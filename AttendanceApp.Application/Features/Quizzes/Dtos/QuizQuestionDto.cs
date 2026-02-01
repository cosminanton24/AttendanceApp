namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record QuizQuestionDto(
    Guid Id,
    Guid QuizId,
    string Text,
    int Order,
    decimal? Points,
    IReadOnlyList<QuizOptionDto> Options
);
