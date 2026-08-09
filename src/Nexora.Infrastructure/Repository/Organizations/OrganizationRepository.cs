using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<Organization>(dbContext, userContext, clock), IOrganizationRepository
{
    public Task<Organization?> GetByIdAsync(Guid organizationId, CancellationToken cancellationToken, bool asTracking = false) =>
        (asTracking ? DbContext.Organizations.IgnoreQueryFilters() : DbContext.Organizations.IgnoreQueryFilters().AsNoTracking()).SingleOrDefaultAsync(
            organization => organization.Id == organizationId,
            cancellationToken);

    public Task AddAsync(Organization organization, CancellationToken cancellationToken) =>
        base.AddEntityAsync(organization, cancellationToken);

    public Task<bool> SlugExistsAsync(OrganizationSlug slug, CancellationToken cancellationToken, Guid? excludingOrganizationId = null) =>
        DbContext.Organizations.IgnoreQueryFilters().AnyAsync(organization => organization.Slug == slug && (!excludingOrganizationId.HasValue || organization.Id != excludingOrganizationId.Value), cancellationToken);

    public Task<List<Organization>> GetAllForManagementAsync(CancellationToken cancellationToken) =>
        DbContext.Organizations.IgnoreQueryFilters().OrderBy(organization => organization.Name).AsNoTracking().ToListAsync(cancellationToken);
}
