using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IOrganizationRoleAssignmentRepository : IBaseRepository<OrganizationRoleAssignment>
{
    Task<OrganizationRoleAssignment?> GetAsync(Guid organizationId, Guid userId, Guid roleId, bool asTracking, CancellationToken cancellationToken);

    Task<List<OrganizationRoleAssignment>> GetByUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);

}
