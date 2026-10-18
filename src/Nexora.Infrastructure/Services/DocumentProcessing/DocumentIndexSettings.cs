using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Infrastructure.Services.Integrations.Embeddings;
using Nexora.Infrastructure.Services.Integrations.VectorStores;

namespace Nexora.Infrastructure.Services.DocumentProcessing;

public sealed class DocumentIndexSettings(
    IOptions<DocumentProcessingOptions> processing,
    IOptions<EmbeddingOptions> embedding,
    IOptions<QdrantOptions> qdrant) : IDocumentIndexSettings
{
    public string ActiveProvider => "Gemini";
    public string IndexName => EmbeddingOptions.ModelName;
    public int BatchSize => processing.Value.EmbeddingBatchSize;
    public int ChunkSizeCharacters => processing.Value.ChunkSizeCharacters;
    public int ChunkOverlapCharacters => processing.Value.ChunkOverlapCharacters;
    public int EmbeddingDimension => embedding.Value.Dimension;
    public int VectorDimension => qdrant.Value.EmbeddingDimension;
    public string CollectionName => qdrant.Value.CollectionName;
    public string EmbeddingModel => EmbeddingOptions.ModelName;
    public string Distance => qdrant.Value.Distance;
}
