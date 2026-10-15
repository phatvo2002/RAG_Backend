namespace Nexora.Application.Common.Abstractions.Documents;

public interface IDocumentStorageSettings
{
    string Bucket { get; }
}

public interface IDocumentIndexSettings
{
    int ChunkSizeCharacters { get; }
    int ChunkOverlapCharacters { get; }
    int EmbeddingDimension { get; }
    int VectorDimension { get; }
    string CollectionName { get; }
    string EmbeddingModel { get; }
    string Distance { get; }
}

public sealed record TextChunk(int Ordinal, string Text, int Start, int End);

public interface IDocumentTextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text, int chunkSize, int overlap);
}
