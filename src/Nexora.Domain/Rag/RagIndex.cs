using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Rag;

public class RagIndex : CreatedEntityBase
{
    protected RagIndex()
    {
    }

    public Guid OrganizationId { get; private set; }

    public string IndexName { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string CollectionName { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = string.Empty;

    public int EmbeddingDimension { get; set; }

    public string DistanceMetric { get; set; } = string.Empty;

    public string ConfigurationHash { get; private set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

}
