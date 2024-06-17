using System.Security.Claims;

namespace Chronofoil.Web.Utils;

public static class Extensions
{
    public static Guid GetCfUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) throw new UnauthorizedAccessException("Failed to get user claim from valid token.");
        return Guid.Parse(userId);
    }
}