using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AttendanceApp.Api.Common.Requests.Quizzes;

public sealed record ActivateQuizForLectureRequest
{
    [JsonRequired]
    public Guid LectureId { get; init; }

    [JsonRequired]
    public Guid QuizId { get; init; }
}
