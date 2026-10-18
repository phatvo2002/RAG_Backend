namespace Nexora.Application.Features.V1.Documents.Model;

public sealed record DocumentUploadResponse(Guid DocumentId, Guid VersionId, string ProcessingStatus);

public sealed record DocumentStatusResponse(Guid DocumentId, Guid VersionId, string FileName, string ProcessingStatus, string? FailureCode, DateTimeOffset CreatedAt, DateTimeOffset? ReadyAt, int ChunkCount);

public sealed record DocumentProgressResponse(Guid DocumentId, Guid VersionId, string FileName, string ProcessingStatus,
    string Provider, string? IndexStatus, int IndexedChunks, int TotalChunks, bool WaitingForQuota, bool Reindexing,
    string? FailureCode, DateTimeOffset CreatedAt, DateTimeOffset? ReadyAt);

public sealed record DocumentListItemResponse(
    Guid DocumentId,
    Guid FolderId,
    string FolderName,
    Guid? DepartmentId,
    string Title,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string DocumentStatus,
    string ProcessingStatus,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ReadyAt);
