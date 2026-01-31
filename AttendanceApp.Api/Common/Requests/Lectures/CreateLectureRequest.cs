using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AttendanceApp.Api.Common.Requests.Lectures;

public sealed record CreateLectureRequest()
{
    [JsonRequired]
    [StringLength(25)]
    public string Name { get; init; } = default!;

    [JsonRequired]
    [StringLength(150)]
    public string Description { get; init; } = default!;

    [JsonRequired]
    public DateTime StartTime { get; init; }

    [JsonRequired]
    public TimeSpan Duration { get; init; }
};