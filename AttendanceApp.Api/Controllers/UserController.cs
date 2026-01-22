using AttendanceApp.Api.Common;
using AttendanceApp.Api.Common.Requests.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CerateUserRequest request, CancellationToken cancellationToken)
    {
        var command = CreateUserRequesToCommand.ToCommand(request);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }
}