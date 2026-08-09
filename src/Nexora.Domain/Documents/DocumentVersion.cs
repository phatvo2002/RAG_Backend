using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Documents;

public class DocumentVersion : CreatedConcurrencyEntityBase
{
    protected DocumentVersion()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid DocumentId { get; private set; }

    public int VersionNo { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string StorageProvider { get; set; } = string.Empty;

    public string StorageBucket { get; set; } = string.Empty;

    public string StorageObjectKey { get; set; } = string.Empty;

    public string? StorageVersionId { get; set; }

    public byte[] FileSha256 { get; private set; } = Array.Empty<byte>();

    public string? ExtractedContentHash { get; set; }

    public string? ParserName { get; set; }

    public string? ParserVersion { get; set; }

    public string ProcessingStatus { get; set; } = string.Empty;

    public string? FailureCode { get; set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset? ReadyAt { get; set; }
}
