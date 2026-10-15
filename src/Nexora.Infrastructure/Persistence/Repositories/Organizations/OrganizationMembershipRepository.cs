using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationMembershipRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<OrganizationMembership>(dbContext, userContext, clock), IOrganizationMembershipRepository
{
    public Task<OrganizationMembership?> GetForAuthenticationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        DbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                membership =>
                    membership.OrganizationId == organizationId &&
                    membership.UserId == userId &&
                    membership.Status == OrganizationMembershipStatus.Active,
                cancellationToken);

    public Task<OrganizationMembership?> GetAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        Query().SingleOrDefaultAsync(
            membership => membership.OrganizationId == organizationId && membership.UserId == userId,
            cancellationToken);

    public Task<OrganizationMembership?> GetForUpdateAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        DbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(membership => membership.OrganizationId == organizationId && membership.UserId == userId, cancellationToken);

    public Task<List<OrganizationMembership>> GetActiveByUsersAsync(Guid organizationId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        DbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(membership => membership.OrganizationId == organizationId &&
                userIds.Contains(membership.UserId) && membership.Status == OrganizationMembershipStatus.Active)
            .ToListAsync(cancellationToken);

}
