namespace Nexora.Application.Common.Abstractions.Documents;

public interface IDocumentStorageSettings
{
    string Bucket { get; }
}

public interface IDocumentIndexSettings
{
    string ActiveProvider { get; }
    string IndexName { get; }
    int BatchSize { get; }
    int ChunkSizeCharacters { get; }
    int ChunkOverlapCharacters { get; }
    int EmbeddingDimension { get; }
    int VectorDimension { get; }
    string CollectionName { get; }
    string EmbeddingModel { get; }
    string Distance { get; }
}

public interface IEmbeddingBatchScheduler
{
    Task WaitForDocumentBatchAsync(CancellationToken cancellationToken);
    void MarkDocumentBatchCompleted();
    Task WaitAfterRateLimitAsync(CancellationToken cancellationToken);
}

public sealed record TextChunk(int Ordinal, string Text, int? Start, int? End, float[]? Embedding = null);

public sealed record DocumentChunkingProgress(string Phase, int Completed, int Total);

public interface IDocumentTextChunker
{
    Task<IReadOnlyList<TextChunk>> ChunkAsync(string text, int chunkSize, int overlap,
        CancellationToken cancellationToken, Action<DocumentChunkingProgress>? reportProgress = null);
}
