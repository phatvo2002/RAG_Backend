using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common;

namespace Nexora.Domain.Documents;

public class DocumentChunkContent : ChunkContentEntityBase
{
    protected DocumentChunkContent()
    {
    }

    private DocumentChunkContent(Guid chunkId, DateTimeOffset now)
        : base(chunkId, now)
    {
    }

    public string StorageProvider { get; set; } = string.Empty;

    public string StorageBucket { get; set; } = string.Empty;

    public string ContentObjectKey { get; set; } = string.Empty;

    public string? StorageVersionId { get; set; }

    public string EncryptionKeyVersion { get; private set; } = string.Empty;

    public string ContentSha256 { get; private set; } = string.Empty;

    public static DocumentChunkContent Create(Guid chunkId, string storageProvider, string storageBucket, string contentObjectKey, string contentSha256, DateTimeOffset now)
    {
        return new DocumentChunkContent(chunkId, now)
        {
            StorageProvider = DomainGuard.RequiredText(storageProvider, nameof(storageProvider), 50),
            StorageBucket = DomainGuard.RequiredText(storageBucket, nameof(storageBucket), 100),
            ContentObjectKey = DomainGuard.RequiredText(contentObjectKey, nameof(contentObjectKey), 1000),
            EncryptionKeyVersion = "none",
            ContentSha256 = DomainGuard.RequiredText(contentSha256, nameof(contentSha256), 64)
        };
    }

    public void ReplaceStorage(string storageProvider, string storageBucket, string contentObjectKey, string contentSha256)
    {
        StorageProvider = DomainGuard.RequiredText(storageProvider, nameof(storageProvider), 50);
        StorageBucket = DomainGuard.RequiredText(storageBucket, nameof(storageBucket), 100);
        ContentObjectKey = DomainGuard.RequiredText(contentObjectKey, nameof(contentObjectKey), 1000);
        ContentSha256 = DomainGuard.RequiredText(contentSha256, nameof(contentSha256), 64);
    }

}
