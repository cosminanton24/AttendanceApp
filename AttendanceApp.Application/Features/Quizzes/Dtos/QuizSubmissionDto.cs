namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record QuizSubmissionDto(
    Guid SubmissionId,
    Guid UserId,
    string UserName,
    string UserEmail,
    bool Submitted,
    DateTime? SubmittedAtUtc,
    decimal Score,
    decimal MaxScore
);
