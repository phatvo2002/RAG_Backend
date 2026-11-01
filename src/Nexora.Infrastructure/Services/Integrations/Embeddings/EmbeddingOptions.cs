namespace Nexora.Infrastructure.Services.Integrations.Embeddings;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    public string Model { get; set; } = "qwen3-embedding:0.6b";
    public int Dimension { get; set; } = 1024;
}
