using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AttendanceApp.Api.Common.Requests.Quizzes;

public sealed record CreateQuizOptionRequest
{
    [JsonRequired]
    public Guid QuestionId { get; init; }

    [JsonRequired]
    [StringLength(512)]
    public string Text { get; init; } = default!;

    [JsonRequired]
    public int Order { get; init; }

    [JsonRequired]
    public bool IsCorrect { get; init; }
}
