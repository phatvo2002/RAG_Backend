using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Rag;

public class RetrievalHit : CreatedEntityBase
{
    protected RetrievalHit()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid RetrievalRunId { get; private set; }

    public Guid ChunkId { get; private set; }

    public int RankNo { get; set; }

    public decimal? SimilarityScore { get; set; }

    public decimal? RerankScore { get; set; }

    public bool SelectedForContext { get; set; }

}
