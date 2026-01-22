using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Users.GetFollowState;

public record GetFollowStateQuery(Guid UserId, Guid TargetId) : IRequest<Result<bool>>;