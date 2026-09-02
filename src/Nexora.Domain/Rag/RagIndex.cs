using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common;

namespace Nexora.Domain.Rag;

public class RagIndex : CreatedEntityBase
{
    protected RagIndex()
    {
    }

    private RagIndex(Guid id, DateTimeOffset now)
        : base(id, now)
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

    public static RagIndex Create(Guid organizationId, string indexName, string provider, string collectionName, string embeddingModel, int embeddingDimension, string distanceMetric, string configurationHash, DateTimeOffset now)
    {
        return new RagIndex(Guid.NewGuid(), now)
        {
            OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId)),
            IndexName = DomainGuard.RequiredText(indexName, nameof(indexName), 150),
            Provider = DomainGuard.RequiredText(provider, nameof(provider), 80),
            CollectionName = DomainGuard.RequiredText(collectionName, nameof(collectionName), 200),
            EmbeddingModel = DomainGuard.RequiredText(embeddingModel, nameof(embeddingModel), 200),
            EmbeddingDimension = embeddingDimension,
            DistanceMetric = DomainGuard.RequiredText(distanceMetric, nameof(distanceMetric), 20),
            ConfigurationHash = DomainGuard.RequiredText(configurationHash, nameof(configurationHash), 64),
            Status = "ACTIVE"
        };
    }

}
