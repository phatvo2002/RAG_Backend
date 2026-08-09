using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Rag;

public class RetrievalRun : CreatedEntityBase
{
    protected RetrievalRun()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid AiTurnId { get; private set; }

    public Guid RagIndexId { get; private set; }

    public string Strategy { get; set; } = string.Empty;

    public int TopK { get; set; }

    public string FilterHash { get; private set; } = string.Empty;

    public string ConfigurationHash { get; private set; } = string.Empty;

    public int? LatencyMs { get; set; }

}
