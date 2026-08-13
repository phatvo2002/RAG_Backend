using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Nexora.Application.Common.Constants;
using Nexora.Domain.Common.Exceptions;
using Nexora.WebApi.Responses;

namespace Nexora.WebApi;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
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

        var details = exception is ValidationException validationException
            ? validationException.Errors
                .Select(error => new ApiErrorDetail(JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName), error.ErrorMessage))
                .ToArray()
            : null;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponseFactory.Failure(httpContext, statusCode, title, code, details),
            cancellationToken);

        return true;
    }
}
