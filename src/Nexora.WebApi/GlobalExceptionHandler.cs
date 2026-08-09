using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.WebApi;

internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, code) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, CommonMessages.Api.RequestValidationFailed, CommonMessages.ErrorCodes.Validation),
            DomainException domainException => (StatusCodes.Status422UnprocessableEntity, domainException.Message, domainException.Code),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, CommonMessages.Api.Forbidden, CommonMessages.ErrorCodes.Forbidden),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, CommonMessages.Api.ConcurrencyConflict, CommonMessages.ErrorCodes.ConcurrencyConflict),
            _ => (StatusCodes.Status500InternalServerError, CommonMessages.Api.UnexpectedError, CommonMessages.ErrorCodes.Server)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, CommonMessages.Api.UnhandledRequestLogTemplate, httpContext.TraceIdentifier);
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
