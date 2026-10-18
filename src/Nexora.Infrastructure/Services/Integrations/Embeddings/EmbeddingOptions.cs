namespace Nexora.Infrastructure.Services.Integrations.Embeddings;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    public const string ModelName = "gemini-embedding-001";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Dimension { get; set; } = 768;
}
