using Nexora.Domain.Documents;
using Nexora.Domain.Rag;

namespace Nexora.Application.Common.Abstractions.Rag;

public interface IDocumentProcessingStore
{
    Task<Document?> GetDocumentAsync(Guid userId, Guid documentId, bool asTracking, CancellationToken cancellationToken);

    Task<Document?> GetDocumentByIdAsync(Guid documentId, bool asTracking, CancellationToken cancellationToken);
    Task<DocumentFolder?> GetFolderAsync(Guid folderId, bool asTracking, CancellationToken cancellationToken);
    Task<bool> CanUploadToFolderAsync(DocumentFolder folder, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<DocumentVersion?> GetVersionAsync(Guid versionId, bool asTracking, CancellationToken cancellationToken);

    Task<int> GetNextVersionNoAsync(Guid documentId, CancellationToken cancellationToken);

    Task<List<DocumentChunk>> GetChunksAsync(Guid versionId, bool asTracking, CancellationToken cancellationToken);

    Task<DocumentChunkContent?> GetChunkContentAsync(Guid chunkId, bool asTracking, CancellationToken cancellationToken);

    Task<RagIndex?> GetIndexAsync(Guid organizationId, string indexName, bool asTracking, CancellationToken cancellationToken);

    Task<RagIndexRecord?> GetIndexRecordAsync(Guid ragIndexId, Guid chunkId, bool asTracking, CancellationToken cancellationToken);

    void InsertDocument(Document document);

    void InsertVersion(DocumentVersion version);

    void InsertChunk(DocumentChunk chunk);

    void InsertChunkContent(DocumentChunkContent content);

    void InsertIndex(RagIndex index);

    void InsertIndexRecord(RagIndexRecord record);
}
