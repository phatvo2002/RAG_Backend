namespace Nexora.Application.Common.Abstractions.Documents;

public interface IObjectStorage
{
    Task PutAsync(string bucket, string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken cancellationToken);
    Task<Stream> GetAsync(string bucket, string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken);
}

public sealed record StoredObjectReference(string StorageProvider, string Bucket, string ObjectKey);
