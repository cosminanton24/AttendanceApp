using AttendanceApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Web.Controllers;

[Route("home")]
public class HomeController : Controller
{
    [HttpGet("index")]
    public IActionResult Index()
    {
        var model = new Models.HomeViewModel
        {
                UserType = UserType.Student
        };
        return View(model);
    }
}
