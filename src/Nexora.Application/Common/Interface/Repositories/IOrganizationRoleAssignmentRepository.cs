using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IOrganizationRoleAssignmentRepository : IBaseRepository<OrganizationRoleAssignment>
{
    Task AddAsync(
        OrganizationRoleAssignment assignment,
        CancellationToken cancellationToken);
}
