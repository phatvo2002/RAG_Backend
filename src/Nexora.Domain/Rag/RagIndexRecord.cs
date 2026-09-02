using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common;

namespace Nexora.Domain.Rag;

public class RagIndexRecord : CreatedEntityBase
{
    protected RagIndexRecord()
    {
    }

    private RagIndexRecord(Guid id, DateTimeOffset now)
        : base(id, now)
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid RagIndexId { get; private set; }

    public Guid ChunkId { get; private set; }

    public string VectorPointId { get; set; } = string.Empty;

    public string ContentHash { get; private set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? IndexedAt { get; set; }

    public static RagIndexRecord Create(Guid organizationId, Guid ragIndexId, Guid chunkId, string vectorPointId, string contentHash, DateTimeOffset now)
    {
        return new RagIndexRecord(Guid.NewGuid(), now)
        {
            OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId)),
            RagIndexId = DomainGuard.RequiredId(ragIndexId, nameof(ragIndexId)),
            ChunkId = DomainGuard.RequiredId(chunkId, nameof(chunkId)),
            VectorPointId = DomainGuard.RequiredText(vectorPointId, nameof(vectorPointId), 200),
            ContentHash = DomainGuard.RequiredText(contentHash, nameof(contentHash), 64),
            Status = "INDEXED",
            IndexedAt = now
        };
    }

    public void MarkIndexed(string contentHash, DateTimeOffset indexedAt)
    {
        ContentHash = DomainGuard.RequiredText(contentHash, nameof(contentHash), 64);
        Status = "INDEXED";
        IndexedAt = indexedAt;
    }

}
