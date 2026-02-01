namespace AttendanceApp.Api.Common.Requests.Quizzes;

public sealed record UpdateQuizOptionRequest
{
    public bool? IsCorrect { get; init; }
    public string? Text { get; init; }
}
