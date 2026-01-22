using AttendanceApp.Api.Common;
using AttendanceApp.Api.Common.Requests.Lectures;
using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Application.Features.Lectures.GetProfessorLectures;
using AttendanceApp.Application.Features.Users.JoinLecture;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Route("api/lectures/")]
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

    [Authorize]
    [HttpGet("{profId:guid}")]
    public async Task<IActionResult> GetProfessorLectures(
        [FromRoute] Guid profId, 
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? fromMonthsAgo = null,
        CancellationToken cancellationToken = default)
    {
        var command = new GetProfessorLecturesQuery(profId, page, pageSize, fromMonthsAgo);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPost("join/{lectureId:guid}")]
    public async Task<IActionResult> JoinLecture([FromRoute] Guid lectureId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = new JoinLectureCommand(userId, lectureId);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }
}