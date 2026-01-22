using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UserController(IMediator mediator) : ControllerBase
{
    
}