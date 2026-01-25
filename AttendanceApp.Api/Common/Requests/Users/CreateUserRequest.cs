using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AttendanceApp.Domain.Enums;

namespace AttendanceApp.Api.Common.Requests.Users;

public sealed record CreateUserRequest()
{
    [JsonRequired]
    [StringLength(100)]
    public string Name { get; init; } = default!;

    [JsonRequired]
    [StringLength(250)]
    public string Email { get; init; } = default!;

    [JsonRequired]
    [StringLength(255, MinimumLength = 8)]
    public string Password { get; init; } = default!;

    [JsonRequired]
    public UserType UserType { get; init; }
};