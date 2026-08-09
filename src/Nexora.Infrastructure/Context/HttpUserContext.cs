using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Nexora.Application.Common.Abstractions;

namespace Nexora.Infrastructure.Context;

internal sealed class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid? UserId => ParseGuid(
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub"));

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
