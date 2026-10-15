using Nexora.Application.Common.Abstractions.Auth;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IRoleRepository
{
    Task<BasePaginationEntity<IdentityRoleSnapshot>> ListPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<IdentityRoleSnapshot?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken);
    Task<IdentityRoleSnapshot?> GetActiveByIdAsync(Guid roleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<IdentityRoleSnapshot>> GetByIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);
    Task<bool> ExistsWithNormalizedNameAsync(string normalizedName, Guid? excludedRoleId, CancellationToken cancellationToken);
    IdentityRoleSnapshot Create(string name, string normalizedName);
    Task RenameAsync(Guid roleId, string name, string normalizedName, CancellationToken cancellationToken);
    Task SetActiveAsync(Guid roleId, bool active, CancellationToken cancellationToken);
}
