using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Web.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    [HttpGet("login")]
    public IActionResult Login() => View();

    [HttpGet("register")]
    public IActionResult Register() => View();
}

