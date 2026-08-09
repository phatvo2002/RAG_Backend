namespace Nexora.Domain.Common.Entities;

public abstract class ChunkContentEntityBase : CompositeUpdateTrackedEntityBase
{
    protected ChunkContentEntityBase()
    {
    }

    public Guid ChunkId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
