using ICU4N.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Documents;
using SemanticChunkerNET;

namespace Nexora.Infrastructure.Services.DocumentProcessing;

public sealed class SemanticDocumentTextChunker(
    IEmbeddingGenerator<string, Embedding<float>> generator,
    IOptions<DocumentProcessingOptions> options) : IDocumentTextChunker
{
    public async Task<IReadOnlyList<TextChunk>> ChunkAsync(
        string text, int chunkSize, int overlap, CancellationToken cancellationToken,
        Action<DocumentChunkingProgress>? reportProgress = null)
    {
        if (chunkSize < 100) throw new InvalidOperationException("ChunkSizeCharacters phải từ 100 trở lên.");
        var settings = options.Value;
        if (settings.SemanticTokenLimit < 1 || settings.SemanticBufferSize < 0 ||
            settings.SemanticBreakpointPercentile is < 0 or > 100)
            throw new InvalidOperationException("Cấu hình semantic chunking không hợp lệ.");

        var sentenceCount = CountSentences(text);
        reportProgress?.Invoke(new DocumentChunkingProgress("ANALYZING", 0, sentenceCount));
        var completed = 0;
        var trackedGenerator = new ProgressEmbeddingGenerator(generator, count =>
        {
            var current = Interlocked.Add(ref completed, count);
            if (current < sentenceCount)
                reportProgress?.Invoke(new DocumentChunkingProgress("ANALYZING", current, sentenceCount));
            else if (current == sentenceCount)
                reportProgress?.Invoke(new DocumentChunkingProgress("BUILDING", 0, 0));
        });
        var chunker = new SemanticChunker(trackedGenerator, settings.SemanticTokenLimit,
            bufferSize: settings.SemanticBufferSize,
            thresholdType: BreakpointThresholdType.Percentile,
            thresholdAmount: settings.SemanticBreakpointPercentile,
            minChunkChars: 0);
        var semanticChunks = await chunker.CreateChunksAsync(text, cancellationToken);
        var result = new List<TextChunk>();
        foreach (var semanticChunk in semanticChunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parts = SplitOversized(semanticChunk.Text, chunkSize);
            foreach (var part in parts)
            {
                var vector = parts.Count == 1
                    ? semanticChunk.Embedding.Vector.ToArray()
                    : (await generator.GenerateAsync(part, cancellationToken: cancellationToken)).Vector.ToArray();
                result.Add(new TextChunk(result.Count, part, null, null, vector));
            }
        }

        var sourcePosition = 0;
        for (var i = 0; i < result.Count; i++)
        {
            var part = result[i];
            int? start = null;
            var end = sourcePosition;
            foreach (var character in part.Text)
            {
                if (char.IsWhiteSpace(character)) continue;
                while (sourcePosition < text.Length && char.IsWhiteSpace(text[sourcePosition])) sourcePosition++;
                if (sourcePosition >= text.Length || text[sourcePosition] != character)
                    throw new InvalidOperationException("Semantic chunker đã thay đổi hoặc làm mất nội dung tài liệu.");
                start ??= sourcePosition;
                end = ++sourcePosition;
            }
            result[i] = part with { Start = start, End = start.HasValue ? end : null };
        }
        if (text[sourcePosition..].Any(character => !char.IsWhiteSpace(character)))
            throw new InvalidOperationException("Semantic chunker đã bỏ sót nội dung tài liệu.");
        return result;
    }

    private static int CountSentences(string text)
    {
        var iterator = BreakIterator.GetSentenceInstance();
        iterator.SetText(text);
        var count = 0;
        for (int start = iterator.First(), end = iterator.Next(); end != BreakIterator.Done;
             start = end, end = iterator.Next())
        {
            if (text.AsSpan(start, end - start).Trim().Length > 0)
                count++;
        }
        return count;
    }

    private sealed class ProgressEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> inner, Action<int> onCompleted)
        : IEmbeddingGenerator<string, Embedding<float>>
    {
        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values, EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var inputs = values.ToArray();
            var result = await inner.GenerateAsync(inputs, options, cancellationToken);
            onCompleted(inputs.Length);
            return result;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }

    private static IReadOnlyList<string> SplitOversized(string text, int limit)
    {
        var parts = new List<string>();
        for (var start = 0; start < text.Length;)
        {
            var end = Math.Min(start + limit, text.Length);
            if (end < text.Length)
            {
                var boundary = text.LastIndexOfAny([' ', '\n', '\r', '\t'], end - 1, end - start);
                if (boundary > start) end = boundary;
            }
            var part = text[start..end].Trim();
            if (part.Length > 0) parts.Add(part);
            start = end;
            while (start < text.Length && char.IsWhiteSpace(text[start])) start++;
        }
        return parts;
    }
}
