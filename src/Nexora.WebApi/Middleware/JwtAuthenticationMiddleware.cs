using Microsoft.Net.Http.Headers;
using Nexora.Application.Common.Constants;
using Nexora.WebApi.Responses;

namespace Nexora.WebApi.Middleware;

internal sealed class JwtAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!ControllerSecurityMetadata.RequiresSecurity(context))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            const int statusCode = StatusCodes.Status401Unauthorized;
            context.Response.StatusCode = statusCode;
            context.Response.Headers[HeaderNames.WWWAuthenticate] = "Bearer";
            await context.Response.WriteAsJsonAsync(
                ApiResponseFactory.Failure(
                    context,
                    statusCode,
                    CommonMessages.Api.AuthenticationRequired,
                    CommonMessages.ErrorCodes.Unauthorized),
                context.RequestAborted);
            return;
        }

        await next(context);
    }
}
