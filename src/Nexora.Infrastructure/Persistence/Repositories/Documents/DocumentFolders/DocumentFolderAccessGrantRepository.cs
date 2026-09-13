using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Domain.Documents;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class DocumentFolderAccessGrantRepository(
    NexoraDbContext dbContext,
    IUserContext userContext,
    IClock clock) : BaseRepository<DocumentFolderAccessGrant>(dbContext, userContext, clock), IDocumentFolderAccessGrantRepository
{
    public Task<List<DocumentFolderAccessGrant>> GetByFolderAsync(Guid organizationId, Guid folderId, CancellationToken cancellationToken) =>
        DbContext.DocumentFolderAccessGrants
            .Where(grant => grant.OrganizationId == organizationId &&
                            grant.FolderId == folderId &&
                            grant.RevokedAt == null)
            .OrderBy(grant => grant.ValidFrom)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<DocumentFolderAccessGrant?> GetByIdAsync(Guid organizationId, Guid folderId, Guid grantId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = DbContext.DocumentFolderAccessGrants
            .IgnoreQueryFilters()
            .Where(grant => grant.OrganizationId == organizationId &&
                            grant.FolderId == folderId &&
                            grant.Id == grantId &&
                            grant.RevokedAt == null);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }
}
