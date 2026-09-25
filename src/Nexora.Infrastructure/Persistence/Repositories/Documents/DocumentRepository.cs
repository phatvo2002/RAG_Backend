using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Results;

namespace Nexora.Infrastructure.Persistence.Repositories.Documents;

internal sealed class DocumentRepository(NexoraDbContext dbContext) : IDocumentRepository
{
    public async Task<PagedResult<DocumentListRecord>> ListAsync(DocumentListFilter filter, CancellationToken cancellationToken)
    {
        var documents = dbContext.Documents.IgnoreQueryFilters();
        var folders = dbContext.DocumentFolders.IgnoreQueryFilters();
        var versions = dbContext.DocumentVersions.IgnoreQueryFilters();
        var grants = dbContext.DocumentFolderAccessGrants.IgnoreQueryFilters();
        var departmentMemberships = dbContext.DepartmentMemberships.IgnoreQueryFilters();

        var query =
            from document in documents
            join folder in folders on document.FolderId equals folder.Id
            join version in versions on document.CurrentVersionId equals version.Id into versionGroup
            from version in versionGroup.DefaultIfEmpty()
            where document.OrganizationId == filter.OrganizationId
                  && folder.OrganizationId == filter.OrganizationId
                  && (version == null || version.OrganizationId == filter.OrganizationId)
            select new { document, folder, version };

        query = query.Where(item =>
            item.document.FolderId == filter.FolderId &&
            item.document.DeletedAt == null &&
            item.folder.DeletedAt == null &&
            (item.document.OwnerUserId == filter.UserId ||
             item.folder.OwnerUserId == filter.UserId ||
             (item.folder.DepartmentId.HasValue && departmentMemberships.Any(membership =>
                 membership.OrganizationId == filter.OrganizationId &&
                 membership.DepartmentId == item.folder.DepartmentId.Value &&
                 membership.UserId == filter.UserId &&
                 membership.LeftAt == null)) ||
             grants.Any(grant =>
                 grant.OrganizationId == filter.OrganizationId &&
                 grant.FolderId == item.folder.Id &&
                 grant.RevokedAt == null &&
                 grant.ValidFrom <= filter.Now &&
                 (grant.ExpiresAt == null || grant.ExpiresAt > filter.Now) &&
                 (grant.UserId == filter.UserId ||
                  (grant.DepartmentId.HasValue && departmentMemberships.Any(membership =>
                      membership.OrganizationId == filter.OrganizationId &&
                      membership.DepartmentId == grant.DepartmentId.Value &&
                      membership.UserId == filter.UserId &&
                       membership.LeftAt == null))))));

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(item => item.folder.DepartmentId == filter.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(item =>
                EF.Functions.ILike(item.document.Title, $"%{search}%") ||
                (item.version != null && EF.Functions.ILike(item.version.OriginalFileName, $"%{search}%")));
        }

        if (!string.IsNullOrWhiteSpace(filter.FileType))
        {
            var fileType = filter.FileType.Trim().TrimStart('.').ToLowerInvariant();
            query = query.Where(item =>
                item.version != null &&
                EF.Functions.ILike(item.version.OriginalFileName, $"%.{fileType}"));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProcessingStatus))
        {
            var status = filter.ProcessingStatus.Trim().ToUpperInvariant();
            query = status == "PROCESSING"
                ? query.Where(item => item.version != null && new[] { "PROCESSING", "EXTRACTING", "CHUNKING", "INDEXING" }.Contains(item.version.ProcessingStatus))
                : query.Where(item => item.version != null && item.version.ProcessingStatus == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.document.UpdatedAt)
            .ThenByDescending(item => item.document.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(item => new DocumentListRecord(
                item.document.Id,
                item.folder.Id,
                item.folder.Name,
                item.folder.DepartmentId ?? item.document.DepartmentId,
                item.document.Title,
                item.version == null ? string.Empty : item.version.OriginalFileName,
                item.version == null ? string.Empty : item.version.MimeType,
                item.version == null ? 0 : item.version.SizeBytes,
                item.document.Status,
                item.version == null ? string.Empty : item.version.ProcessingStatus,
                item.version == null ? null : item.version.FailureCode,
                item.document.CreatedAt,
                item.document.UpdatedAt,
                item.version == null ? null : item.version.ReadyAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentListRecord>(items, filter.Page, filter.PageSize, totalCount);
    }
}
