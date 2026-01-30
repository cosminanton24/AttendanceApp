
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AttendanceApp.Domain.Enums;

namespace AttendanceApp.Application.Common.Jwt;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(id))
            throw new UnauthorizedAccessException("Missing user id claim.");

        if (!Guid.TryParse(id, out var guid))
            throw new UnauthorizedAccessException("Invalid user id claim.");

        return guid;
    }

    public static UserType GetUserType(this ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(role))
            throw new UnauthorizedAccessException("Missing user type claim.");

        if (!Enum.TryParse<UserType>(role, out var userType))
            throw new UnauthorizedAccessException("Invalid user type claim.");

        return userType;
    }
}