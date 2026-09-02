using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Domain.Documents;
using Nexora.Domain.Rag;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class DocumentProcessingStore(NexoraDbContext dbContext) : IDocumentProcessingStore
{
    public Task<Document?> GetDocumentAsync(Guid userId, Guid documentId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.Documents.IgnoreQueryFilters().Where(document => document.Id == documentId && document.OwnerUserId == userId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Document?> GetDocumentByIdAsync(Guid documentId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = dbContext.Documents.IgnoreQueryFilters().Where(document => document.Id == documentId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
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

    public void InsertDocument(Document document) => dbContext.Documents.Add(document);
    public void InsertVersion(DocumentVersion version) => dbContext.DocumentVersions.Add(version);
    public void InsertChunk(DocumentChunk chunk) => dbContext.DocumentChunks.Add(chunk);
    public void InsertChunkContent(DocumentChunkContent content) => dbContext.DocumentChunkContents.Add(content);
    public void InsertIndex(RagIndex index) => dbContext.RagIndexes.Add(index);
    public void InsertIndexRecord(RagIndexRecord record) => dbContext.RagIndexRecords.Add(record);
}
