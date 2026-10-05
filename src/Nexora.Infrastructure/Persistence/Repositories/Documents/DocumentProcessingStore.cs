using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Domain.Documents;
using Nexora.Domain.Rag;
using DomainDocument = Nexora.Domain.Documents.Document;

namespace Nexora.Infrastructure.Persistence.Repositories.Documents;

internal sealed class DocumentProcessingStore(NexoraDbContext dbContext) : IDocumentProcessingStore
{
    public Task<DomainDocument?> GetDocumentAsync(Guid userId, Guid documentId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.Documents.IgnoreQueryFilters().Where(document => document.Id == documentId && document.OwnerUserId == userId && document.DeletedAt == null);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<DomainDocument?> GetDocumentByIdAsync(Guid documentId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.Documents.IgnoreQueryFilters().Where(document => document.Id == documentId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<DocumentFolder?> GetFolderAsync(Guid folderId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.DocumentFolders.Where(folder => folder.Id == folderId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentVersion>> GetDocumentVersionsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) =>
        await dbContext.DocumentVersions.IgnoreQueryFilters()
            .Where(version => version.OrganizationId == organizationId && version.DocumentId == documentId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StoredObjectReference>> GetChunkStorageObjectsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) =>
        await (from content in dbContext.DocumentChunkContents.IgnoreQueryFilters()
               join chunk in dbContext.DocumentChunks.IgnoreQueryFilters() on content.ChunkId equals chunk.Id
               join version in dbContext.DocumentVersions.IgnoreQueryFilters() on chunk.DocumentVersionId equals version.Id
               where version.OrganizationId == organizationId && version.DocumentId == documentId
               select new StoredObjectReference(content.StorageProvider, content.StorageBucket, content.ContentObjectKey))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VectorPointReference>> GetDocumentVectorPointsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken)
    {
        var references = await (from index in dbContext.RagIndexes.IgnoreQueryFilters()
                                join version in dbContext.DocumentVersions.IgnoreQueryFilters() on index.OrganizationId equals version.OrganizationId
                                join chunk in dbContext.DocumentChunks.IgnoreQueryFilters() on version.Id equals chunk.DocumentVersionId
                                where index.OrganizationId == organizationId && version.DocumentId == documentId
                                select new { index.CollectionName, chunk.Id })
            .Distinct()
            .ToListAsync(cancellationToken);

        return references
            .Select(reference => new VectorPointReference(reference.CollectionName, reference.Id.ToString("D")))
            .ToArray();
    }

    public async Task MarkDocumentVectorRecordsDeletedAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken)
    {
        var records = await (from record in dbContext.RagIndexRecords.IgnoreQueryFilters()
                             join chunk in dbContext.DocumentChunks.IgnoreQueryFilters() on record.ChunkId equals chunk.Id
                             join version in dbContext.DocumentVersions.IgnoreQueryFilters() on chunk.DocumentVersionId equals version.Id
                             where record.OrganizationId == organizationId && version.OrganizationId == organizationId && version.DocumentId == documentId
                             select record)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            record.MarkDeleted();
        }
    }

    public async Task<bool> CanUploadToFolderAsync(DocumentFolder folder, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        if (folder.OwnerUserId == userId)
        {
            return true;
        }

        var departmentMemberships = dbContext.DepartmentMemberships.IgnoreQueryFilters();
        if (folder.DepartmentId.HasValue && await departmentMemberships.AnyAsync(membership =>
                membership.OrganizationId == folder.OrganizationId &&
                membership.DepartmentId == folder.DepartmentId.Value &&
                membership.UserId == userId &&
                membership.LeftAt == null,
            cancellationToken))
        {
            return true;
        }

        var grants = dbContext.DocumentFolderAccessGrants.IgnoreQueryFilters();
        return await grants.AnyAsync(grant =>
            grant.OrganizationId == folder.OrganizationId &&
            grant.FolderId == folder.Id &&
            grant.RevokedAt == null &&
            grant.ValidFrom <= utcNow &&
            (grant.ExpiresAt == null || grant.ExpiresAt > utcNow) &&
            EF.Functions.ILike(grant.AccessLevel, "WRITE") &&
            (grant.UserId == userId ||
             (grant.DepartmentId.HasValue && departmentMemberships.Any(membership =>
                 membership.OrganizationId == folder.OrganizationId &&
                 membership.DepartmentId == grant.DepartmentId.Value &&
                 membership.UserId == userId &&
                 membership.LeftAt == null))),
            cancellationToken);
    }
            
    public Task<DocumentVersion?> GetVersionAsync(Guid versionId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.DocumentVersions.IgnoreQueryFilters().Where(version => version.Id == versionId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetNextVersionNoAsync(Guid documentId, CancellationToken cancellationToken) => (await dbContext.DocumentVersions.IgnoreQueryFilters().Where(version => version.DocumentId == documentId).Select(version => (int?)version.VersionNo).MaxAsync(cancellationToken) ?? 0) + 1;

    public async Task<List<DocumentChunk>> GetChunksAsync(Guid versionId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.DocumentChunks.IgnoreQueryFilters().Where(chunk => chunk.DocumentVersionId == versionId).OrderBy(chunk => chunk.Ordinal);
        return asTracking ? await query.ToListAsync(cancellationToken) : await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<DocumentChunkContent?> GetChunkContentAsync(Guid chunkId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.DocumentChunkContents.Where(content => content.ChunkId == chunkId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<RagIndex?> GetIndexAsync(Guid organizationId, string indexName, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.RagIndexes.IgnoreQueryFilters().Where(index => index.OrganizationId == organizationId && index.IndexName == indexName);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<RagIndexRecord?> GetIndexRecordAsync(Guid ragIndexId, Guid chunkId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.RagIndexRecords.IgnoreQueryFilters().Where(record => record.RagIndexId == ragIndexId && record.ChunkId == chunkId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public void InsertDocument(DomainDocument document) => dbContext.Documents.Add(document);
    public void InsertVersion(DocumentVersion version) => dbContext.DocumentVersions.Add(version);
    public void InsertChunk(DocumentChunk chunk) => dbContext.DocumentChunks.Add(chunk);
    public void InsertChunkContent(DocumentChunkContent content) => dbContext.DocumentChunkContents.Add(content);
    public void InsertIndex(RagIndex index) => dbContext.RagIndexes.Add(index);
    public void InsertIndexRecord(RagIndexRecord record) => dbContext.RagIndexRecords.Add(record);
}
