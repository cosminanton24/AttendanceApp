namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record QuizzesPageDto(
    IReadOnlyList<QuizDto> Items,
    int Total
);
