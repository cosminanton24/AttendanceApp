namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record QuizSubmissionsPageDto(
    IReadOnlyList<QuizSubmissionDto> Submissions,
    int TotalCount,
    int PageNumber,
    int PageSize
);
