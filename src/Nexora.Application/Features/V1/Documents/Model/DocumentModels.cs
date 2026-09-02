using Microsoft.AspNetCore.Http;

namespace Nexora.Application.Features.V1.Documents.Model;

public sealed class UploadDocumentRequest
{
    public IFormFile File { get; set; } = null!;
}

public sealed record DocumentUploadResponse(Guid DocumentId, Guid VersionId, string ProcessingStatus);

public sealed record DocumentStatusResponse(Guid DocumentId, Guid VersionId, string FileName, string ProcessingStatus, string? FailureCode, DateTimeOffset CreatedAt, DateTimeOffset? ReadyAt, int ChunkCount);
