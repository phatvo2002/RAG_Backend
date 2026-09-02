using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Abstractions.Rag;

public sealed record DocumentUploadReceipt(Guid DocumentId, Guid VersionId, string ProcessingStatus);

public sealed record DocumentStatusSnapshot(Guid DocumentId, Guid VersionId, string FileName, string ProcessingStatus, string? FailureCode, DateTimeOffset CreatedAt, DateTimeOffset? ReadyAt, int ChunkCount);

public interface IDocumentIngestionService
{
    Task<Result<DocumentUploadReceipt>> UploadAsync(Guid userId, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken);

    Task<DocumentStatusSnapshot?> GetStatusAsync(Guid userId, Guid documentId, CancellationToken cancellationToken);
}

public interface IDocumentProcessingQueue
{
    ValueTask EnqueueAsync(Guid versionId, CancellationToken cancellationToken);
}
