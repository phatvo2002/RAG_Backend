using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Permissions;

namespace Nexora.WebApi.Middleware;

internal sealed class ApiPermissionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IApiRequestPermissionChecker permissionChecker)
    {
        if (!ControllerSecurityMetadata.RequiresSecurity(context))
        {
            await next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint is not RouteEndpoint routeEndpoint ||
            endpoint.Metadata.GetMetadata<IHttpMethodMetadata>() is not { HttpMethods.Count: > 0 } methodMetadata)
        {
            throw new UnauthorizedAccessException(CommonMessages.Api.Forbidden);
        }

        var routeTemplate = NormalizeRouteTemplate(routeEndpoint.RoutePattern.RawText);
        if (routeTemplate is null)
        {
            throw new UnauthorizedAccessException(CommonMessages.Api.Forbidden);
        }

        var httpMethod = context.Request.Method;
        if (!methodMetadata.HttpMethods.Any(method => string.Equals(method, httpMethod, StringComparison.OrdinalIgnoreCase)))
        {
            throw new UnauthorizedAccessException(CommonMessages.Api.Forbidden);
        }

        var request = new ApiRequestDescriptor(httpMethod, routeTemplate);
        if (!await permissionChecker.HasAccessAsync(request, context.RequestAborted))
        {
            throw new UnauthorizedAccessException(CommonMessages.Api.Forbidden);
        }

        await next(context);
    }

    private static string? NormalizeRouteTemplate(string? rawRouteTemplate)
    {
        if (string.IsNullOrWhiteSpace(rawRouteTemplate))
        {
            return null;
        }

        var routeTemplate = rawRouteTemplate.Trim();
        if (!routeTemplate.StartsWith('/'))
        {
            routeTemplate = $"/{routeTemplate}";
        }

        routeTemplate = routeTemplate.TrimEnd('/');
        return routeTemplate.Length == 0 ? "/" : routeTemplate;
    }
}
