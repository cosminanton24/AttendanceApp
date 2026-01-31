using AttendanceApp.Domain.Enums;

namespace AttendanceApp.Web.Models;

public class ProfileViewModel
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserType UserType { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsOwnProfile { get; set; }
    public bool IsFollowing { get; set; }
}
