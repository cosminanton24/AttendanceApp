using AttendanceApp.Application.Features.Users.CreateUser;

namespace AttendanceApp.Api.Common.Requests.Users;

public static class CreateUserRequesToCommand
{
    public static CreateUserCommand ToCommand(CerateUserRequest request)
    {
        return new CreateUserCommand(
            request.Name,
            request.Email,
            request.Password,
            request.Type
        );
    }
}