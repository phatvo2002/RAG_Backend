using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Application.Common.Behaviors;

public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(
                exception,
                "Invalid argument while handling request {RequestType}.",
                typeof(TRequest).Name);

            throw new ValidationException(exception.Message);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (DomainException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception while handling request {RequestType}.",
                typeof(TRequest).Name);

            throw;
        }
    }
}
