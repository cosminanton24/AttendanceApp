using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AttendanceApp.Api.Common.Requests.Quizzes;

public sealed record CreateQuizQuestionRequest
{
    [JsonRequired]
    public Guid QuizId { get; init; }

    [JsonRequired]
    [StringLength(1024)]
    public string Text { get; init; } = default!;

    [JsonRequired]
    public int Order { get; init; }

    public decimal? Points { get; init; }
}
