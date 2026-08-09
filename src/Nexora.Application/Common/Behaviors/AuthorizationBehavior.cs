using MediatR;
using Nexora.Application.Common.Constants;

namespace Nexora.Application.Common.Behaviors;

public interface IRequirePermission
{
    string PermissionCode { get; }
}

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(
        string permissionCode,
        CancellationToken cancellationToken);
}

public sealed class AuthorizationBehavior<TRequest, TResponse>(IPermissionChecker permissionChecker)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IRequirePermission permissionRequest &&
            !await permissionChecker.HasPermissionAsync(permissionRequest.PermissionCode, cancellationToken))
        {
            throw new UnauthorizedAccessException(
                string.Format(CommonMessages.Api.PermissionNotGrantedTemplate, permissionRequest.PermissionCode));
        }

        return await next(cancellationToken);
    }
}
