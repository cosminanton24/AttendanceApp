namespace AttendanceApp.Api.Common.Requests.Quizzes;

public sealed record UpdateQuizQuestionRequest
{
    public string? Text { get; init; }
    public decimal? Points { get; init; }
}
