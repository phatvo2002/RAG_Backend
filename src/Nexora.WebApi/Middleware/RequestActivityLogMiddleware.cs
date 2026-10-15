using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Nexora.Application.Common.Abstractions.Logging;

namespace Nexora.WebApi.Middleware;

internal sealed class RequestActivityLogMiddleware(RequestDelegate next, ILogger<RequestActivityLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IActivityLogWriter activityLogWriter)
    {
        if (!ShouldLogRequest(context.Request.Method))
        {
            await next(context);
            return;
        }

        var startedTimestamp = Stopwatch.GetTimestamp();
        Exception? requestException = null;
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            requestException = exception;
            throw;
        }
        finally
        {
            var statusCode = requestException is not null ? StatusCodes.Status500InternalServerError : context.Response.StatusCode == 0 ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;
            var routeTemplate = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
            var entry = new ActivityLogEntry(
                ParseUserId(context), null, "HTTP", context.Request.Method, "Request",
                requestException is null && statusCode < 400 ? "SUCCESS" : "FAILURE", Truncate(context.Request.Path.Value, 500),
                context.Connection.RemoteIpAddress?.ToString(), Truncate(routeTemplate, 300), statusCode,
                (long)Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds);
            try
            {
                await activityLogWriter.WriteAsync(entry, CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not persist ActivityLog for request {CorrelationId}.", context.TraceIdentifier);
            }
        }
    }

    private static bool ShouldLogRequest(string method) => HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);

    private static Guid? ParseUserId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var value = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static string? Truncate(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : value.Length <= maxLength ? value : value[..maxLength];
}
