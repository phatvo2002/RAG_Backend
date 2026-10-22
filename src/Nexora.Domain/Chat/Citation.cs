using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Chat;

public class Citation : CreatedEntityBase
{
    protected Citation()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid AssistantMessageId { get; private set; }

    public Guid? ChunkId { get; private set; }

    public int CitationNo { get; set; }

    public int? PageNo { get; set; }

    public int? CharStart { get; set; }

    public int? CharEnd { get; set; }

    public float? Score { get; set; }

    public string? QuoteHash { get; private set; }

    public static Citation Create(Guid id, Guid organizationId, Guid assistantMessageId, Guid chunkId,
        int citationNo, int? pageNo, int? charStart, int? charEnd, float score, DateTimeOffset now) => new(id, now)
    {
        OrganizationId = organizationId,
        AssistantMessageId = assistantMessageId,
        ChunkId = chunkId,
        CitationNo = citationNo,
        PageNo = pageNo,
        CharStart = charStart,
        CharEnd = charEnd,
        Score = score
    };

    private Citation(Guid id, DateTimeOffset now) : base(id, now) { }

}
