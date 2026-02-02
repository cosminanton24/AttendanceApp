using System.Text.Json.Serialization;

namespace AttendanceApp.Api.Common.Requests.Quizzes;

public sealed record SaveUserAnswerRequest
{
    [JsonRequired]
    public Guid QuizLectureId { get; init; }

    [JsonRequired]
    public Guid QuestionId { get; init; }

    [JsonRequired]
    public Guid OptionId { get; init; }

    [JsonRequired]
    public bool Choice { get; init; }
}
