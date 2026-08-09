using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Documents;

public class DocumentChunkContent : ChunkContentEntityBase
{
    protected DocumentChunkContent()
    {
    }

    public string StorageProvider { get; set; } = string.Empty;

    public string StorageBucket { get; set; } = string.Empty;

    public string ContentObjectKey { get; set; } = string.Empty;

    public string? StorageVersionId { get; set; }

    public string EncryptionKeyVersion { get; private set; } = string.Empty;

    public string ContentSha256 { get; private set; } = string.Empty;

}
