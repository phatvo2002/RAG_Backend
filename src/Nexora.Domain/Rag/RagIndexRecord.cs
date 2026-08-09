using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Rag;

public class RagIndexRecord : CreatedEntityBase
{
    protected RagIndexRecord()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid RagIndexId { get; private set; }

    public Guid ChunkId { get; private set; }

    public string VectorPointId { get; set; } = string.Empty;

    public string ContentHash { get; private set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? IndexedAt { get; set; }

}
