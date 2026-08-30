namespace Nexora.Application.Common.Abstractions.Rag;

public interface IObjectStorage
{
    Task PutAsync(string bucket, string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken cancellationToken);

    Task<Stream> GetAsync(string bucket, string objectKey, CancellationToken cancellationToken);
}

public interface IEmbeddingProvider
{
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken);
}

public sealed record VectorPoint(string Id, float[] Vector, Guid DocumentId, Guid VersionId, Guid ChunkId, string ContentHash, string FileName);

public interface IVectorStore
{
    Task EnsureCollectionAsync(string collectionName, int dimension, string distance, CancellationToken cancellationToken);

    Task UpsertAsync(string collectionName, IReadOnlyCollection<VectorPoint> points, CancellationToken cancellationToken);
}
