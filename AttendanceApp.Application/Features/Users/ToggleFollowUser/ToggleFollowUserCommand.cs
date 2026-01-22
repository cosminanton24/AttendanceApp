using AttendanceApp.Application.Common.Results;
using MediatR;

namespace AttendanceApp.Application.Features.Users.ToggleFollowUser;

public record ToggleFollowUserCommand(Guid UserId, Guid TargetId) : IRequest<Result<bool>>;