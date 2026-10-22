using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Results;

namespace Nexora.Infrastructure.Persistence.Repositories.Documents;

internal sealed class DocumentRepository(NexoraDbContext dbContext) : IDocumentRepository, IFolderOverviewRepository, IDocumentRetrievalStore, IDocumentChunkReadStore
{
    public async Task<PagedResult<DocumentChunkReadRecord>?> GetAccessiblePageAsync(
        Guid organizationId, Guid userId, DateTimeOffset now, Guid documentId,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var documents = dbContext.Documents.IgnoreQueryFilters();
        var folders = dbContext.DocumentFolders.IgnoreQueryFilters();
        var versions = dbContext.DocumentVersions.IgnoreQueryFilters();
        var grants = dbContext.DocumentFolderAccessGrants.IgnoreQueryFilters();
        var memberships = dbContext.DepartmentMemberships.IgnoreQueryFilters();

        var versionId = await (from document in documents
                               join folder in folders on document.FolderId equals folder.Id
                               join version in versions on document.CurrentVersionId equals version.Id
                               where document.Id == documentId &&
                                     document.OrganizationId == organizationId &&
                                     folder.OrganizationId == organizationId &&
                                     version.OrganizationId == organizationId &&
                                     version.DocumentId == document.Id &&
                                     document.DeletedAt == null && folder.DeletedAt == null &&
                                     (document.OwnerUserId == userId || folder.OwnerUserId == userId ||
                                      (folder.DepartmentId.HasValue && memberships.Any(membership =>
                                          membership.OrganizationId == organizationId &&
                                          membership.DepartmentId == folder.DepartmentId.Value &&
                                          membership.UserId == userId && membership.LeftAt == null)) ||
                                      grants.Any(grant =>
                                          grant.OrganizationId == organizationId &&
                                          grant.FolderId == folder.Id && grant.RevokedAt == null &&
                                          grant.ValidFrom <= now && (grant.ExpiresAt == null || grant.ExpiresAt > now) &&
                                          (grant.UserId == userId ||
                                           (grant.DepartmentId.HasValue && memberships.Any(membership =>
                                               membership.OrganizationId == organizationId &&
                                               membership.DepartmentId == grant.DepartmentId.Value &&
                                               membership.UserId == userId && membership.LeftAt == null)))))
                               select (Guid?)version.Id).SingleOrDefaultAsync(cancellationToken);
        if (!versionId.HasValue) return null;

        var query = from chunk in dbContext.DocumentChunks.IgnoreQueryFilters()
                    join content in dbContext.DocumentChunkContents.IgnoreQueryFilters() on chunk.Id equals content.ChunkId
                    where chunk.OrganizationId == organizationId && chunk.DocumentVersionId == versionId.Value &&
                          chunk.Status == "ACTIVE"
                    select new { Chunk = chunk, Content = content };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.Chunk.Ordinal)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new DocumentChunkReadRecord(item.Chunk.Id, item.Chunk.Ordinal, item.Chunk.PageNo,
                item.Chunk.CharStart, item.Chunk.CharEnd, item.Chunk.LanguageCode,
                item.Content.StorageBucket, item.Content.ContentObjectKey))
            .ToListAsync(cancellationToken);
        return new PagedResult<DocumentChunkReadRecord>(items, page, pageSize, totalCount);
    }

    public Task<string?> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken) =>
        dbContext.Organizations.IgnoreQueryFilters().AsNoTracking()
            .Where(organization => organization.Id == organizationId)
            .Select(organization => organization.Name)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<IndexedDocumentReference>> GetIndexedDocumentsAsync(
        Guid organizationId,
        string indexName,
        CancellationToken cancellationToken)
    {
        var documents = dbContext.Documents.IgnoreQueryFilters();
        var folders = dbContext.DocumentFolders.IgnoreQueryFilters();
        var versions = dbContext.DocumentVersions.IgnoreQueryFilters();
        var chunks = dbContext.DocumentChunks.IgnoreQueryFilters();
        var records = dbContext.RagIndexRecords.IgnoreQueryFilters();
        var indexes = dbContext.RagIndexes.IgnoreQueryFilters();

        var query =
            from document in documents
            join folder in folders on document.FolderId equals folder.Id
            join version in versions on document.CurrentVersionId equals version.Id
            join chunk in chunks on version.Id equals chunk.DocumentVersionId
            join record in records on chunk.Id equals record.ChunkId
            join index in indexes on record.RagIndexId equals index.Id
            where document.OrganizationId == organizationId &&
                  folder.OrganizationId == organizationId &&
                  version.OrganizationId == organizationId &&
                  record.OrganizationId == organizationId &&
                  index.OrganizationId == organizationId &&
                  index.IndexName == indexName &&
                  document.DeletedAt == null &&
                  folder.DeletedAt == null &&
                  version.ProcessingStatus == "READY" &&
                  chunk.Status == "ACTIVE" &&
                  record.Status == "INDEXED" &&
                  index.Status == "ACTIVE"
            select new IndexedDocumentReference(document.Id, folder.DepartmentId.HasValue);

        return await query.Distinct().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAccessibleIndexedDocumentIdsAsync(
        Guid organizationId,
        Guid userId,
        DateTimeOffset now,
        string indexName,
        CancellationToken cancellationToken)
    {
        var documents = dbContext.Documents.IgnoreQueryFilters();
        var folders = dbContext.DocumentFolders.IgnoreQueryFilters();
        var versions = dbContext.DocumentVersions.IgnoreQueryFilters();
        var chunks = dbContext.DocumentChunks.IgnoreQueryFilters();
        var records = dbContext.RagIndexRecords.IgnoreQueryFilters();
        var indexes = dbContext.RagIndexes.IgnoreQueryFilters();
        var grants = dbContext.DocumentFolderAccessGrants.IgnoreQueryFilters();
        var departmentMemberships = dbContext.DepartmentMemberships.IgnoreQueryFilters();

        var query =
            from document in documents
            join folder in folders on document.FolderId equals folder.Id
            join version in versions on document.CurrentVersionId equals version.Id
            join chunk in chunks on version.Id equals chunk.DocumentVersionId
            join record in records on chunk.Id equals record.ChunkId
            join index in indexes on record.RagIndexId equals index.Id
            where document.OrganizationId == organizationId &&
                  folder.OrganizationId == organizationId &&
                  version.OrganizationId == organizationId &&
                  record.OrganizationId == organizationId &&
                  index.OrganizationId == organizationId &&
                  index.IndexName == indexName &&
                  document.DeletedAt == null &&
                  folder.DeletedAt == null &&
                  version.ProcessingStatus == "READY" &&
                  chunk.Status == "ACTIVE" &&
                  record.Status == "INDEXED" &&
                  index.Status == "ACTIVE" &&
                  (document.OwnerUserId == userId ||
                   folder.OwnerUserId == userId ||
                   (folder.DepartmentId.HasValue && departmentMemberships.Any(membership =>
                       membership.OrganizationId == organizationId &&
                       membership.DepartmentId == folder.DepartmentId!.Value &&
                       membership.UserId == userId &&
                       membership.LeftAt == null)) ||
                   grants.Any(grant =>
                       grant.OrganizationId == organizationId &&
                       grant.FolderId == folder.Id &&
                       grant.RevokedAt == null &&
                       grant.ValidFrom <= now &&
                       (grant.ExpiresAt == null || grant.ExpiresAt > now) &&
                       (grant.UserId == userId ||
                        (grant.DepartmentId.HasValue && departmentMemberships.Any(membership =>
                            membership.OrganizationId == organizationId &&
                            membership.DepartmentId == grant.DepartmentId.Value &&
                            membership.UserId == userId &&
                            membership.LeftAt == null)))))
            select document.Id;

        return await query.Distinct().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RetrievalChunkSource>> GetAccessibleChunkSourcesAsync(
        Guid organizationId,
        Guid userId,
        DateTimeOffset now,
        IReadOnlyCollection<Guid> chunkIds,
        CancellationToken cancellationToken)
    {
        if (chunkIds.Count == 0)
        {
            return Array.Empty<RetrievalChunkSource>();
        }

        var documents = dbContext.Documents.IgnoreQueryFilters();
        var folders = dbContext.DocumentFolders.IgnoreQueryFilters();
        var versions = dbContext.DocumentVersions.IgnoreQueryFilters();
        var chunks = dbContext.DocumentChunks.IgnoreQueryFilters();
        var contents = dbContext.DocumentChunkContents.IgnoreQueryFilters();
        var records = dbContext.RagIndexRecords.IgnoreQueryFilters();
        var indexes = dbContext.RagIndexes.IgnoreQueryFilters();
        var grants = dbContext.DocumentFolderAccessGrants.IgnoreQueryFilters();
        var departmentMemberships = dbContext.DepartmentMemberships.IgnoreQueryFilters();
        var departments = dbContext.Departments.IgnoreQueryFilters();

        var query =
            from document in documents
            join folder in folders on document.FolderId equals folder.Id
            join version in versions on document.CurrentVersionId equals version.Id
            join chunk in chunks on version.Id equals chunk.DocumentVersionId
            join content in contents on chunk.Id equals content.ChunkId
            join record in records on chunk.Id equals record.ChunkId
            join index in indexes on record.RagIndexId equals index.Id
            join department in departments on folder.DepartmentId equals (Guid?)department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            where chunkIds.Contains(chunk.Id) &&
                  document.OrganizationId == organizationId &&
                  folder.OrganizationId == organizationId &&
                  version.OrganizationId == organizationId &&
                  record.OrganizationId == organizationId &&
                  index.OrganizationId == organizationId &&
                  document.DeletedAt == null &&
                  folder.DeletedAt == null &&
                  (department == null || department.OrganizationId == organizationId) &&
                  version.ProcessingStatus == "READY" &&
                  chunk.Status == "ACTIVE" &&
                  record.Status == "INDEXED" &&
                  index.Status == "ACTIVE" &&
                  (document.OwnerUserId == userId ||
                   folder.OwnerUserId == userId ||
                   (folder.DepartmentId.HasValue && departmentMemberships.Any(membership =>
                       membership.OrganizationId == organizationId &&
                       membership.DepartmentId == folder.DepartmentId!.Value &&
                       membership.UserId == userId &&
                       membership.LeftAt == null)) ||
                   grants.Any(grant =>
                       grant.OrganizationId == organizationId &&
                       grant.FolderId == folder.Id &&
                       grant.RevokedAt == null &&
                       grant.ValidFrom <= now &&
                       (grant.ExpiresAt == null || grant.ExpiresAt > now) &&
                       (grant.UserId == userId ||
                        (grant.DepartmentId.HasValue && departmentMemberships.Any(membership =>
                            membership.OrganizationId == organizationId &&
                            membership.DepartmentId == grant.DepartmentId.Value &&
                            membership.UserId == userId &&
                            membership.LeftAt == null)))))
            select new RetrievalChunkSource(
                document.Id,
                chunk.Id,
                version.OriginalFileName,
                department == null ? null : department.Name,
                chunk.PageNo,
                chunk.CharStart,
                chunk.CharEnd,
                content.StorageBucket,
                content.ContentObjectKey);

        return await query.Distinct().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FolderDocumentSummary>> GetFolderSummariesAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> folderIds,
        CancellationToken cancellationToken)
    {
        if (folderIds.Count == 0)
        {
            return Array.Empty<FolderDocumentSummary>();
        }

        var documents = dbContext.Documents.IgnoreQueryFilters();
        var versions = dbContext.DocumentVersions.IgnoreQueryFilters();

        var currentDocuments =
            from document in documents
            join version in versions on document.CurrentVersionId equals version.Id into versionGroup
            from version in versionGroup.DefaultIfEmpty()
            where document.OrganizationId == organizationId &&
                  document.FolderId.HasValue &&
                  folderIds.Contains(document.FolderId.Value) &&
                  document.DeletedAt == null &&
                  (version == null || version.OrganizationId == organizationId)
            select new
            {
                FolderId = document.FolderId!.Value,
                SizeBytes = version == null ? 0L : version.SizeBytes,
                ProcessingStatus = version == null ? string.Empty : version.ProcessingStatus,
                document.UpdatedAt
            };

        return await currentDocuments
            .GroupBy(item => item.FolderId)
            .Select(group => new FolderDocumentSummary(
                group.Key,
                group.Count(),
                group.Sum(item => item.SizeBytes),
                group.Count(item => item.ProcessingStatus == "READY"),
                group.Count(item => item.ProcessingStatus != "READY" && item.ProcessingStatus != "FAILED" && item.ProcessingStatus != "STOPPED"),
                group.Count(item => item.ProcessingStatus == "FAILED" || item.ProcessingStatus == "STOPPED"),
                group.Max(item => (DateTimeOffset?)item.UpdatedAt)))
            .ToListAsync(cancellationToken);
    }

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
            (!filter.FolderId.HasValue || item.document.FolderId == filter.FolderId.Value) &&
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
                ? query.Where(item => item.version != null && new[] { "PROCESSING", "EXTRACTING", "CHUNKING", "INDEXING", "STOPPING" }.Contains(item.version.ProcessingStatus))
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
