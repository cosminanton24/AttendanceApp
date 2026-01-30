using AttendanceApp.Api.Common;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Application.Features.Users.GetFollowState;
using AttendanceApp.Application.Features.Users.GetUserInfoBatch;
using AttendanceApp.Application.Features.Users.ToggleFollowUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = CreateUserRequesToCommand.ToCommand(request);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginUser([FromBody] LoginUserRequest request, CancellationToken cancellationToken)
    {
        var command = LoginUserRequesToCommand.ToCommand(request);
        var result = await mediator.Send(command, cancellationToken);

        if(result.IsSuccess)
        {
            Response.Cookies.Append("AttendanceApp.Jwt", result.Value, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });
        }     
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("following/{profId:guid}")]
    public async Task<IActionResult> GetFollowState([FromRoute] Guid profId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var query = new GetFollowStateQuery(userId, profId);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("toggleFollow/{profId:guid}")]
    public async Task<IActionResult> ToggleFollowUser([FromRoute] Guid profId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new ToggleFollowUserCommand(userId, profId);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpGet("userInfo")]
    public async Task<IActionResult> GetUserInfo([FromQuery] Guid[] ids, CancellationToken cancellationToken)
    {
        var query = new GetUserInfoBatchQuery(ids);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }
}