using AttendanceApp.Api.Common;
using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Application.Features.Users.UserFollowings.GetFollowers;
using AttendanceApp.Application.Features.Users.UserFollowings.GetFollowing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/userFollowings")]
public sealed class UserFollowingsController(IMediator mediator) : ControllerBase
{
    [HttpGet("following/{userId:guid}")]
    public async Task<IActionResult> GetFollowing(
        [FromRoute] Guid userId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFollowingQuery(userId, pageIndex, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("followers/{userId:guid}")]
    public async Task<IActionResult> GetFollowers(
        [FromRoute] Guid userId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFollowersQuery(userId, pageIndex, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }
}
