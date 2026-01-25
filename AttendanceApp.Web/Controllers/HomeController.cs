using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Web.Controllers;

[Route("home")]
public class HomeController : Controller
{
    [HttpGet("index")]
    public IActionResult Index() => Content("OK - Web is running");
}
