using AttendanceApp.Api.Common;
using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Application.Features.LectureAttendees.GetLectureAttendees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lectureAttendees")]

public sealed class LectureAttendeesController(IMediator mediator) : ControllerBase
{
    [HttpGet("{lectureId:guid}")]
    public async Task<IActionResult> GetLectureAttendees(
        [FromRoute] Guid lectureId,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var query = new GetLectureAttendeesQuery(userId, lectureId, page, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return this.ToActionResult(result);
    }
}
