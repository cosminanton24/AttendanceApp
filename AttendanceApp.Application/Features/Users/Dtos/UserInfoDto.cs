namespace AttendanceApp.Application.Features.Users.Dtos;

public sealed record UserInfoDto(
    Guid Id,
    string Name,
    string Email
);
