namespace AttendanceApp.Application.Features.Quizzes.Dtos;

public sealed record StudentQuizResultDto(
    Guid QuizLectureId,
    Guid QuizId,
    string QuizName,
    DateTime ActivatedAtUtc,
    bool HasSubmitted,
    decimal? Score,
    decimal? MaxScore
);
