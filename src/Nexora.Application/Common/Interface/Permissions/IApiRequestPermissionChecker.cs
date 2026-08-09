namespace Nexora.Application.Common.Permissions;

public sealed record ApiRequestDescriptor(string HttpMethod, string RouteTemplate);

public interface IApiRequestPermissionChecker
{
    Task<bool> HasAccessAsync(ApiRequestDescriptor request, CancellationToken cancellationToken);
}
