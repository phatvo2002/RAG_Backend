using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Domain.Documents;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class DocumentFolderRepository(
    NexoraDbContext dbContext,
    IUserContext userContext,
    IClock clock) : BaseRepository<DocumentFolder>(dbContext, userContext, clock), IDocumentFolderRepository
{
    public Task<DocumentFolder?> GetByIdAsync(Guid folderId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = DbContext.DocumentFolders
            .IgnoreQueryFilters()
            .Where(folder => folder.Id == folderId && folder.DeletedAt == null);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<DocumentFolder>> GetForOrganizationAsync(Guid organizationId, Guid? departmentId, CancellationToken cancellationToken) =>
        DbContext.DocumentFolders
            .Where(folder => folder.OrganizationId == organizationId &&
                             (!departmentId.HasValue || folder.DepartmentId == departmentId.Value))
            .OrderBy(folder => folder.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsInDepartmentAsync(Guid organizationId, Guid departmentId, Guid folderId, CancellationToken cancellationToken) =>
        DbContext.DocumentFolders
            .AnyAsync(folder => folder.OrganizationId == organizationId &&
                                folder.DepartmentId == departmentId &&
                                folder.Id == folderId,
                cancellationToken);

    public Task<bool> HasActiveDocumentsAsync(Guid folderId, CancellationToken cancellationToken) =>
        DbContext.Documents.AnyAsync(document => document.FolderId == folderId, cancellationToken);
}
