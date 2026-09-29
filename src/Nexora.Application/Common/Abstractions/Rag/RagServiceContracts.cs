namespace Nexora.Application.Common.Abstractions.Rag;

public interface IObjectStorage
{
    Task PutAsync(string bucket, string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken cancellationToken);

    Task<Stream> GetAsync(string bucket, string objectKey, CancellationToken cancellationToken);

    Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken);
}

public interface IEmbeddingProvider
{
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken);
}

public sealed record VectorPoint(string Id, float[] Vector, Guid DocumentId, Guid VersionId, Guid ChunkId, string ContentHash, string FileName);

public sealed record StoredObjectReference(string StorageProvider, string Bucket, string ObjectKey);

public sealed record VectorPointReference(string CollectionName, string PointId);

public sealed record VectorSearchResult(Guid DocumentId, Guid ChunkId, float Score);

public sealed record RetrievalChunkSource(
    Guid DocumentId,
    Guid ChunkId,
    string FileName,
    int? PageNo,
    int? CharStart,
    int? CharEnd,
    string StorageBucket,
    string ContentObjectKey);

public interface IDocumentRetrievalStore
{
    Task<IReadOnlyList<Guid>> GetAccessibleIndexedDocumentIdsAsync(Guid organizationId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<IReadOnlyList<RetrievalChunkSource>> GetAccessibleChunkSourcesAsync(
        Guid organizationId,
        Guid userId,
        DateTimeOffset now,
        IReadOnlyCollection<Guid> chunkIds,
        CancellationToken cancellationToken);
}

public interface IVectorStore
{
    Task EnsureCollectionAsync(string collectionName, int dimension, string distance, CancellationToken cancellationToken);

    Task UpsertAsync(string collectionName, IReadOnlyCollection<VectorPoint> points, CancellationToken cancellationToken);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        float[] vector,
        IReadOnlyCollection<Guid> allowedDocumentIds,
        int limit,
        CancellationToken cancellationToken);

    Task DeleteAsync(string collectionName, IReadOnlyCollection<string> pointIds, CancellationToken cancellationToken);
}
