using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Permissions;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.ValueObjects;

namespace Nexora.Infrastructure.Services.Authorization;

internal sealed class ApiPermissionService(
    IApiFunctionRepository functions,
    IDefaultOrganizationProvider organizations,
    IUserContext userContext,
    IClock clock) : IApiRequestPermissionChecker
{
    public async Task<bool> HasAccessAsync(ApiRequestDescriptor request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryParseHttpMethod(request.HttpMethod, out var httpMethod))
        {
            return false;
        }

        var routeTemplate = NormalizeRouteTemplate(request.RouteTemplate);
        if (routeTemplate is null)
        {
            return false;
        }

        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
            return false;
        var organizationId = await organizations.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
            return false;

        var canonicalRoute = RouteTemplate.Create(routeTemplate);
        var effective = await functions.GetEffectiveByUserAsync(organizationId.Value, userContext.UserId.Value,
            clock.UtcNow, cancellationToken);
        return effective.Any(function =>
            (function.HttpMethod == httpMethod || function.HttpMethod == ApiHttpMethod.Any) &&
            function.RouteTemplate == canonicalRoute);
    }

    private static bool TryParseHttpMethod(string value, out ApiHttpMethod httpMethod)
    {
        return Enum.TryParse(value.Trim(), ignoreCase: true, out httpMethod) && httpMethod != ApiHttpMethod.Any;
    }

    private static string? NormalizeRouteTemplate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var route = value.Trim();
        if (!route.StartsWith('/'))
        {
            route = $"/{route}";
        }

        route = route.TrimEnd('/');
        return route.Length == 0 || route.Length > 300 || route.Contains('?') || route.Contains('#') || route.Any(char.IsWhiteSpace)
            ? null
            : route;
    }
}
