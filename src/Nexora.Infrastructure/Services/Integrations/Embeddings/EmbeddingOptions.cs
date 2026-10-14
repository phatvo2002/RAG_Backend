namespace Nexora.Infrastructure.Services.Integrations.Embeddings;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-3-small";
    public int Dimension { get; set; } = 1536;
}
