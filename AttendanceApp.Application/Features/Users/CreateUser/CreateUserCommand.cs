using AttendanceApp.Application.Common.Results;
using AttendanceApp.Domain.Enums;
using MediatR;

namespace AttendanceApp.Application.Features.Users.CreateUser;

public record CreateUserCommand(string Name, string Email, string Password, UserType Type) : IRequest<Result>;