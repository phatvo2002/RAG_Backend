using Nexora.Application.Common.Abstractions.Documents;

namespace Nexora.Infrastructure.Services.DocumentProcessing;

public sealed class DocumentTextChunker : IDocumentTextChunker
{
    public IReadOnlyList<TextChunk> Chunk(string text, int chunkSize, int overlap)
    {
        if (chunkSize < 100 || overlap < 0 || overlap >= chunkSize)
            throw new InvalidOperationException("DocumentProcessing chunk configuration không hợp lệ.");

        var result = new List<TextChunk>();
        for (var start = 0; start < text.Length;)
        {
            var length = Math.Min(chunkSize, text.Length - start);
            var end = start + length;
            if (end < text.Length)
            {
                var boundary = text.LastIndexOfAny([' ', '\n', '\r', '\t'], end - 1, Math.Min(200, length));
                if (boundary > start + (chunkSize / 2)) end = boundary;
            }

            var value = text[start..end].Trim();
            if (!string.IsNullOrWhiteSpace(value))
                result.Add(new TextChunk(result.Count, value, start, end));
            if (end >= text.Length) break;
            start = Math.Max(start + 1, end - overlap);
        }
        return result;
    }
}
