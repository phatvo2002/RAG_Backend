using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IOrganizationMembershipRepository : IBaseRepository<OrganizationMembership>
{
    Task<OrganizationMembership?> GetForAuthenticationAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<OrganizationMembership?> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken);

}
