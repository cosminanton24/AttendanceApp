using AttendanceApp.Application.Common.Results;
using AttendanceApp.Application.Features.Users.Dtos;
using MediatR;

namespace AttendanceApp.Application.Features.Users.GetUserInfoBatch;

public sealed record GetUserInfoBatchQuery(IReadOnlyList<Guid> Ids)
    : IRequest<Result<IReadOnlyList<UserInfoDto>>>;
