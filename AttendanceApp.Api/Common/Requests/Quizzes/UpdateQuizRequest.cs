namespace AttendanceApp.Api.Common.Requests.Quizzes;

public sealed record UpdateQuizRequest
{
    public string? Name { get; init; }
    public string? Duration { get; init; }
}
