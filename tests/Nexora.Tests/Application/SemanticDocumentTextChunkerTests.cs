using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Infrastructure.Services.DocumentProcessing;
using Nexora.Infrastructure.Services.Integrations.Embeddings;

namespace Nexora.Tests.Application;

public sealed class SemanticDocumentTextChunkerTests
{
    [Theory]
    [InlineData("Hợp đồng có hiệu lực từ hôm nay. Bên mua thanh toán trong ba ngày. Đây là điều khoản cuối.")]
    [InlineData("The agreement begins today. Payment is due in three days. This is the final clause.")]
    [InlineData("Câu lặp. Câu lặp. Câu lặp.\n\nĐoạn tiếp theo có nhiều khoảng trắng.")]
    public async Task Preserves_text_order_and_offsets(string text)
    {
        var chunker = CreateChunker(new FakeProvider());

        var chunks = await chunker.ChunkAsync(text, 1200, 0, default);

        Assert.NotEmpty(chunks);
        Assert.Equal(Squash(text), Squash(string.Concat(chunks.Select(chunk => chunk.Text))));
        Assert.All(chunks, chunk =>
        {
            Assert.True(chunk.Text.Length <= 1200);
            Assert.NotNull(chunk.Embedding);
            Assert.Equal(Squash(chunk.Text), Squash(text[chunk.Start!.Value..chunk.End!.Value]));
        });
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(chunk => chunk.Ordinal));
    }

    [Fact]
    public async Task Splits_a_single_long_sentence_without_losing_characters()
    {
        var text = new string('A', 2501);
        var provider = new FakeProvider();
        var chunks = await CreateChunker(provider).ChunkAsync(text, 1200, 0, default);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(text, string.Concat(chunks.Select(chunk => chunk.Text)));
        Assert.All(chunks, chunk => Assert.InRange(chunk.Text.Length, 1, 1200));
        Assert.True(provider.Calls >= 4);
    }

    [Fact]
    public async Task Splits_at_a_topic_change()
    {
        var text = string.Join(' ', Enumerable.Repeat("Contract terms are binding.", 5)
            .Concat(Enumerable.Repeat("Invoice details are payable.", 5)));
        var chunks = await CreateChunker(new FakeProvider(distinguishTopics: true), percentile: 50)
            .ChunkAsync(text, 1200, 0, default);

        Assert.True(chunks.Count > 1);
        Assert.Equal(Squash(text), Squash(string.Concat(chunks.Select(chunk => chunk.Text))));
    }

    [Fact]
    public async Task Reports_completed_sentence_embeddings_before_chunk_assembly()
    {
        var progress = new List<DocumentChunkingProgress>();
        var text = "First sentence. Second sentence. Third sentence.";

        await CreateChunker(new FakeProvider()).ChunkAsync(text, 1200, 0, default, progress.Add);

        Assert.Equal(new DocumentChunkingProgress("ANALYZING", 0, 3), progress[0]);
        Assert.Contains(progress, item => item.Phase == "ANALYZING" && item.Completed > 0 && item.Completed < 3);
        Assert.Contains(progress, item => item.Phase == "BUILDING");
    }

    [Fact]
    public void Progress_store_ignores_late_updates_and_clears_completed_work()
    {
        var store = new DocumentChunkingProgressStore();
        var versionId = Guid.NewGuid();
        store.Report(versionId, new DocumentChunkingProgress("ANALYZING", 2, 5));
        store.Report(versionId, new DocumentChunkingProgress("ANALYZING", 1, 5));
        Assert.Equal(2, store.Get(versionId)!.Completed);

        store.Report(versionId, new DocumentChunkingProgress("BUILDING", 0, 0));
        store.Report(versionId, new DocumentChunkingProgress("ANALYZING", 5, 5));
        Assert.Equal("BUILDING", store.Get(versionId)!.Phase);

        store.Report(versionId, new DocumentChunkingProgress("SAVING", 3, 3));
        store.Clear(versionId);
        Assert.Null(store.Get(versionId));
    }

    [Fact]
    public async Task Cancelled_chunking_stops_before_embedding()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateChunker(new FakeProvider()).ChunkAsync("Một câu.", 1200, 0, cancellation.Token));
    }

    [Fact]
    public async Task Fixed_fallback_preserves_overlap_and_needs_no_embedding()
    {
        var chunks = await new DocumentTextChunker().ChunkAsync(new string('X', 300), 200, 50, default);

        Assert.Equal(2, chunks.Count);
        Assert.Equal(0, chunks[0].Start);
        Assert.Equal(150, chunks[1].Start);
        Assert.All(chunks, chunk => Assert.Null(chunk.Embedding));
    }

    private static SemanticDocumentTextChunker CreateChunker(FakeProvider provider, double percentile = 95) =>
        new(new GeminiEmbeddingGeneratorAdapter(provider, new FakeScheduler()),
            Options.Create(new DocumentProcessingOptions
            {
                SemanticTokenLimit = 320,
                SemanticBreakpointPercentile = percentile
            }), Microsoft.Extensions.Logging.Abstractions.NullLogger<SemanticDocumentTextChunker>.Instance);

    private static string Squash(string text) => new(text.Where(character => !char.IsWhiteSpace(character)).ToArray());

    private sealed class FakeProvider(bool distinguishTopics = false) : IEmbeddingProvider
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult<IReadOnlyList<float[]>>(inputs.Select(input =>
                distinguishTopics && input.Contains("Invoice", StringComparison.OrdinalIgnoreCase)
                    ? new float[] { 0, 1 }
                    : new float[] { 1, 0 }).ToArray());
        }
    }

    private sealed class FakeScheduler : IEmbeddingBatchScheduler
    {
        public Task WaitForDocumentBatchAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void MarkDocumentBatchCompleted() { }
        public Task WaitAfterRateLimitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
