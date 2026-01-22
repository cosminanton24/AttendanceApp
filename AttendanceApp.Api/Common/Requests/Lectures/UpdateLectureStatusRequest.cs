using System.Text.Json.Serialization;
using AttendanceApp.Domain.Enums;

namespace AttendanceApp.Api.Common.Requests.Lectures;

public sealed record UpdateLectureStatusRequest()
{
    [JsonRequired]
    public LectureStatus Status { get; init; }
};