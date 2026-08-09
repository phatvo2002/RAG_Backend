using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Chat;

public class Citation : CreatedEntityBase
{
    protected Citation()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid AssistantMessageId { get; private set; }

    public Guid ChunkId { get; private set; }

    public int CitationNo { get; set; }

    public int? PageNo { get; set; }

    public int? CharStart { get; set; }

    public int? CharEnd { get; set; }

    public string? QuoteHash { get; private set; }

}
