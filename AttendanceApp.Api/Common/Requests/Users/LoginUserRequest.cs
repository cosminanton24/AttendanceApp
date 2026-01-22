using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AttendanceApp.Api.Common.Requests.Users;

public sealed record LoginUserRequest()
{
    [JsonRequired]
    [StringLength(250)]
    public string Email { get; init; } = default!;

    [JsonRequired]
    [StringLength(255)]
    public string Password { get; init; } = default!;
};