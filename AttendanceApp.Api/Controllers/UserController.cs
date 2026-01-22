using AttendanceApp.Api.Common;
using AttendanceApp.Api.Common.Requests.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Route("api/users/")]
public class UserController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> CreateUser([FromBody] CerateUserRequest request, CancellationToken cancellationToken)
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
}