using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IOrganizationMembershipRepository : IBaseRepository<OrganizationMembership>
{
    Task<OrganizationMembership?> GetForAuthenticationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);

    Task<OrganizationMembership?> GetAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);

    Task<OrganizationMembership?> GetForUpdateAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);

    Task<List<OrganizationMembership>> GetActiveByUsersAsync(Guid organizationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

}
