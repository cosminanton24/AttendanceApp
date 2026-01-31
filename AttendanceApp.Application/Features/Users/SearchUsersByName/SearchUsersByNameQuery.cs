using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Users.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Users.SearchUsersByName;

public sealed record SearchUsersByNameQuery(
    string Name,
    int Page,
    int PageSize
) : IRequest<Result<UsersPageDto>>;
