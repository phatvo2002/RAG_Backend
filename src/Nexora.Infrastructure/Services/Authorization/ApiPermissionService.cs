using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Permissions;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Services.Authorization;

internal sealed class ApiPermissionService(NexoraDbContext dbContext) : IApiRequestPermissionChecker
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

        var canonicalRoute = RouteTemplate.Create(routeTemplate);
        return await dbContext.ApiFunctions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                function =>
                    function.Status == ApiFunctionStatus.Active &&
                    (function.HttpMethod == httpMethod || function.HttpMethod == ApiHttpMethod.Any) &&
                    function.RouteTemplate == canonicalRoute,
                cancellationToken);
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
