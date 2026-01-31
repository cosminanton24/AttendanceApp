using AttendanceApp.Application.Common.Jwt;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApp.Web.Controllers;

[Route("profile")]
public class ProfileController(
    IUserRepository userRepository,
    IUserFollowingsRepository userFollowingsRepository) : Controller
{
    [HttpGet("view/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> View(Guid userId, CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        
        // Redirect to /profile/me if viewing own profile
        if (currentUserId == userId)
        {
            return RedirectToAction(nameof(Me));
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        var followersCount = await userFollowingsRepository.GetTotalFollowersAsync(userId, cancellationToken);
        var followingCount = await userFollowingsRepository.GetTotalFollowingAsync(userId, cancellationToken);

        var following = await userFollowingsRepository.GetFollowingAsync(currentUserId, userId, cancellationToken);
        var isFollowing = following != null;

        var model = new ProfileViewModel
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            UserType = user.Type,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsOwnProfile = false,
            IsFollowing = isFollowing
        };

        return View(model);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var currentUserId = User.GetUserId();
        var user = await userRepository.GetByIdAsync(currentUserId, cancellationToken);
        
        if (user == null)
        {
            return NotFound();
        }

        var followersCount = await userFollowingsRepository.GetTotalFollowersAsync(currentUserId, cancellationToken);
        var followingCount = await userFollowingsRepository.GetTotalFollowingAsync(currentUserId, cancellationToken);

        var model = new ProfileViewModel
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            UserType = user.Type,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsOwnProfile = true,
            IsFollowing = false
        };

        return View(model);
    }
}
