using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common;

namespace Nexora.Domain.Documents;

public class DocumentVersion : CreatedConcurrencyEntityBase
{
    protected DocumentVersion()
    {
    }

    private DocumentVersion(Guid id, DateTimeOffset now)
        : base(id, now)
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

    public static DocumentVersion Create(Guid organizationId, Guid documentId, int versionNo, string originalFileName, string mimeType, long sizeBytes, string storageProvider, string storageBucket, string storageObjectKey, byte[] fileSha256, Guid createdByUserId, DateTimeOffset now)
    {
        var version = new DocumentVersion(Guid.NewGuid(), now)
        {
            OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId)),
            DocumentId = DomainGuard.RequiredId(documentId, nameof(documentId)),
            VersionNo = versionNo,
            OriginalFileName = DomainGuard.RequiredText(originalFileName, nameof(originalFileName), 500),
            MimeType = DomainGuard.RequiredText(mimeType, nameof(mimeType), 150),
            SizeBytes = sizeBytes,
            StorageProvider = DomainGuard.RequiredText(storageProvider, nameof(storageProvider), 50),
            StorageBucket = DomainGuard.RequiredText(storageBucket, nameof(storageBucket), 100),
            StorageObjectKey = DomainGuard.RequiredText(storageObjectKey, nameof(storageObjectKey), 1000),
            FileSha256 = fileSha256 is { Length: 32 } ? fileSha256 : throw new ArgumentException("SHA-256 must contain 32 bytes.", nameof(fileSha256)),
            CreatedByUserId = DomainGuard.RequiredId(createdByUserId, nameof(createdByUserId)),
            ProcessingStatus = "QUEUED"
        };
        return version;
    }

    public void SetProcessingStatus(string status, string? failureCode = null, DateTimeOffset? readyAt = null)
    {
        ProcessingStatus = DomainGuard.RequiredText(status, nameof(status), 30);
        FailureCode = failureCode;
        ReadyAt = readyAt;
    }
}
