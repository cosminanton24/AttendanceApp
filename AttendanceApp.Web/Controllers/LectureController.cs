using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Web.Controllers;

[Route("lecture")]
public class LectureController : Controller
{
    [HttpGet("join/{Id:guid}")]
    [Authorize]
    public IActionResult Join([FromRoute] Guid Id)
    {
        ViewData["LectureId"] = Id;
        return View();
    }
}
