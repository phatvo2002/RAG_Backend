using Nexora.Domain.Common;

namespace Nexora.Domain.Common.Entities;

public abstract class ChunkContentEntityBase : CompositeUpdateTrackedEntityBase
{
    protected ChunkContentEntityBase()
    {
    }

    protected ChunkContentEntityBase(Guid chunkId, DateTimeOffset now)
    {
        ChunkId = DomainGuard.RequiredId(chunkId, nameof(chunkId));
        CreatedAt = DomainGuard.Utc(now, nameof(now));
        UpdatedAt = CreatedAt;
        RowVersion = 1;
    }

    public Guid ChunkId { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }
}
