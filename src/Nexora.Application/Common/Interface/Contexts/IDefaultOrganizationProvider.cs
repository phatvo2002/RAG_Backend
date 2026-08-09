namespace Nexora.Application.Common.Abstractions;

public interface IDefaultOrganizationProvider
{
    Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken);
}
