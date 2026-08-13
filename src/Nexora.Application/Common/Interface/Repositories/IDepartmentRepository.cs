using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IDepartmentRepository : IBaseRepository<Department>
{
    Task<Department?> GetByIdAsync(Guid departmentId, bool asTracking, CancellationToken cancellationToken);

    Task<bool> CodeExistsAsync(Guid organizationId, string code, Guid? excludingDepartmentId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid departmentId, Guid organizationId, CancellationToken cancellationToken);

    Task<List<Department>> GetAllForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

}
