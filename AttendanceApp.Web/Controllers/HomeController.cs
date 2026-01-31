using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Domain.Enums;
using AttendanceApp.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Web.Controllers;

[Route("home")]
public class HomeController(IUserRepository userRepository) : Controller
{
    [HttpGet("index")]
    [Authorize]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var userType = User.GetUserType();
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        
        var model = new Models.HomeViewModel
        {
            UserType = userType,
            UserName = user?.Name ?? ""
        };
        return View(model);
    }
}
