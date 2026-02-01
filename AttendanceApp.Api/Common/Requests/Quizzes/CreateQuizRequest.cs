using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AttendanceApp.Api.Common.Requests.Quizzes;

public sealed record CreateQuizRequest
{
    [JsonRequired]
    [StringLength(256)]
    public string Name { get; init; } = default!;

    [JsonRequired]
    public TimeSpan Duration { get; init; }
}
