using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IDepartmentMembershipRepository : IBaseRepository<DepartmentMembership>
{
    Task<DepartmentMembership?> GetAsync(Guid organizationId, Guid departmentId, Guid userId, bool asTracking, CancellationToken cancellationToken);

    Task<List<DepartmentMembership>> GetByDepartmentAsync(Guid organizationId, Guid departmentId, CancellationToken cancellationToken);

}
