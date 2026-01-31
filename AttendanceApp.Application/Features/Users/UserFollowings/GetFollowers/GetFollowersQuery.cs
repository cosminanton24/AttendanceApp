using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Users.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Users.UserFollowings.GetFollowers;

public sealed record GetFollowersQuery(
    Guid UserId,
    int PageIndex,
    int PageSize
) : IRequest<Result<UsersPageDto>>;
