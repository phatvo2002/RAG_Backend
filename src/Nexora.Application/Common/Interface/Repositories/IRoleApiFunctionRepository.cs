using Nexora.Domain.Authorization;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IRoleApiFunctionRepository : IBaseRepository<RoleApiFunction>
{
    Task<RoleApiFunction?> GetAsync(Guid organizationId, Guid roleId, Guid functionId, bool asTracking, CancellationToken cancellationToken);

    Task<List<RoleApiFunction>> GetByRoleAsync(Guid organizationId, Guid roleId, CancellationToken cancellationToken);

}
