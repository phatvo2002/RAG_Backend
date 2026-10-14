namespace Nexora.Infrastructure.Services.Integrations.VectorStores;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";
    public string Endpoint { get; set; } = "http://localhost:6333";
    public string ApiKey { get; set; } = string.Empty;
    public string CollectionName { get; set; } = "nexora_documents";
    public int EmbeddingDimension { get; set; } = 1536;
    public string Distance { get; set; } = "Cosine";
}
