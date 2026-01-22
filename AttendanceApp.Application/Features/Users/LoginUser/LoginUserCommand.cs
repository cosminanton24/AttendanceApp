using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Users.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<Result<string>>;