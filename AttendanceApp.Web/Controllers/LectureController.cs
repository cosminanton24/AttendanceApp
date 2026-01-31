using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Web.Controllers;

[Route("lecture")]
public class LectureController : Controller
{
    [HttpGet("join/{id?}")]
    [Authorize]
    public IActionResult Join([FromRoute] string? id)
    {
        id ??= string.Empty;

        var isValid = Guid.TryParse(id, out _);
        ViewData["LectureId"] = id;
        ViewData["IsLectureIdValid"] = isValid;
        return View();
    }
}
