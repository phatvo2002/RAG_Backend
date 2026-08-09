using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IOrganizationRepository : IBaseRepository<Organization>
{
    Task<Organization?> GetByIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken,
        bool asTracking = false);

    Task AddAsync(
        Organization organization,
        CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(
        OrganizationSlug slug,
        CancellationToken cancellationToken,
        Guid? excludingOrganizationId = null);

    Task<List<Organization>> GetAllForManagementAsync(CancellationToken cancellationToken);
}
