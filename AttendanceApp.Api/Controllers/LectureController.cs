using AttendanceApp.Api.Common;
using AttendanceApp.Api.Common.Requests.Lectures;
using AttendanceApp.Api.Common.Requests.Users;
using AttendanceApp.Application.Common.Jwt;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Route("api/lectures")]
public class LectureController(IMediator mediator) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateLecture([FromBody] CerateLectureRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = CerateLectureRequestToCommand.ToCommand(userId, request);
        var result = await mediator.Send(command, cancellationToken);

        return this.ToActionResult(result);
    }

}