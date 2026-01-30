using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Web.Controllers;

[Route("home")]
public class HomeController : Controller
{
    [HttpGet("index")]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userType = User.GetUserType();
        var model = new Models.HomeViewModel
        {
            UserType = userType
        };
        return View(model);
    }
}
