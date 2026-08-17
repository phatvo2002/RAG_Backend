using System.Security.Claims;

namespace Nexora.WebApi.RateLimiting;

internal static class RateLimitPartitionKeys
{
    public static string Global(HttpContext context) => $"global:{UserOrIp(context)}";

    public static string Auth(HttpContext context) => $"auth:ip:{GetIpAddress(context)}";

    public static string Write(HttpContext context) => $"write:{UserOrIp(context)}";

    private static string UserOrIp(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(userId, out var parsedUserId) ? $"user:{parsedUserId}" : $"ip:{GetIpAddress(context)}";
    }

    private static string GetIpAddress(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
