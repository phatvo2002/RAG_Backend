using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.WebApi;

internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, code) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Request validation failed.", "validation.error"),
            DomainException domainException => (StatusCodes.Status422UnprocessableEntity, domainException.Message, domainException.Code),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "The requested operation is not permitted.", "authorization.forbidden"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The resource was changed by another operation.", "concurrency.conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "server.error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing request {TraceIdentifier}.", httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = $"https://httpstatuses.com/{statusCode}",
                Instance = httpContext.Request.Path,
                Extensions = { ["code"] = code, ["traceId"] = httpContext.TraceIdentifier }
            }
        });

        return true;
    }
}
