namespace AttendanceApp.Application.Features.Users.Dtos;

public sealed record UsersPageDto(
    IReadOnlyList<UserInfoDto> Items,
    int Total
);
