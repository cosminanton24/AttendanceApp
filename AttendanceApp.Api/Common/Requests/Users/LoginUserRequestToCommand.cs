using AttendanceApp.Application.Features.Users.CreateUser;
using AttendanceApp.Application.Features.Users.LoginUser;

namespace AttendanceApp.Api.Common.Requests.Users;

public static class LoginUserRequesToCommand
{
    public static LoginUserCommand ToCommand(LoginUserRequest request)
    {
        return new LoginUserCommand(
            request.Email,
            request.Password
        );
    }
}