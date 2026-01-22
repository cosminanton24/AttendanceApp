using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AttendanceApp.Api.Common.Requests.Lectures;

public sealed record CerateLectureRequest()
{
    [JsonRequired]
    [StringLength(100)]
    public string Name { get; } = default!;

    [JsonRequired]
    [StringLength(500)]
    public string Description { get; } = default!;

    [JsonRequired]
    public DateTime StartTime { get; }

    [JsonRequired]
    public TimeSpan Duration { get; }
};