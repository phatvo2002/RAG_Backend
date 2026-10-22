using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Abstractions.Documents;

public sealed record DocumentChunkReadRecord(
    Guid ChunkId, int Ordinal, int? PageNo, int? CharStart, int? CharEnd,
    string? LanguageCode, string StorageBucket, string ContentObjectKey);

public interface IDocumentChunkReadStore
{
    Task<PagedResult<DocumentChunkReadRecord>?> GetAccessiblePageAsync(
        Guid organizationId, Guid userId, DateTimeOffset now, Guid documentId,
        int page, int pageSize, CancellationToken cancellationToken);
}
