using AttendanceApp.Api.Common;
using AttendanceApp.Api.Common.Requests.Lectures;
using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Application.Features.Lectures.GetProfessorLectures;
using AttendanceApp.Application.Features.Lectures.GetStudentLectures;
using AttendanceApp.Application.Features.Users.JoinLecture;
using AttendanceApp.Domain.Enums;
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
    public async Task<IActionResult> CreateLecture([FromBody] CreateLectureRequest request, CancellationToken cancellationToken)
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
        [FromQuery] int page = 0,
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

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetStudentLectures(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? fromMonthsAgo = null,
        [FromQuery] LectureStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();

        var query = new GetStudentLecturesQuery(userId, page, pageSize, fromMonthsAgo, status);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [Authorize]
    [HttpPut("status/{lectureId:guid}")]
    public async Task<IActionResult> UpdateLectureStatus([FromRoute] Guid lectureId, [FromBody] UpdateLectureStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var command = UpdateLectureStatusRequestToCommand.ToCommand(userId, lectureId, request);
        var result = await mediator.Send(command, cancellationToken);
        return this.ToActionResult(result);
    }
}