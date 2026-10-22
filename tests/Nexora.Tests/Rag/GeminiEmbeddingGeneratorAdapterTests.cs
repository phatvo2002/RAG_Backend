using System.Net;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Infrastructure.Services.Integrations.Embeddings;

namespace Nexora.Tests.Rag;

public sealed class GeminiEmbeddingGeneratorAdapterTests
{
    [Fact]
    public async Task Retries_429_without_changing_input()
    {
        var provider = new FakeProvider();
        var scheduler = new FakeScheduler();
        using var adapter = new GeminiEmbeddingGeneratorAdapter(provider, scheduler);

        var result = await adapter.GenerateAsync(["câu gốc"]);

        Assert.Equal(2, provider.Calls);
        Assert.Equal(["câu gốc", "câu gốc"], provider.Inputs);
        Assert.Equal(1, scheduler.Retries);
        Assert.Equal(2, Assert.Single(result).Vector.Length);
    }

    [Fact]
    public async Task Cancellation_is_forwarded_to_provider()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var adapter = new GeminiEmbeddingGeneratorAdapter(new FakeProvider(), new FakeScheduler());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            adapter.GenerateAsync(["text"], cancellationToken: cancellation.Token));
    }

    private sealed class FakeProvider : IEmbeddingProvider
    {
        public int Calls { get; private set; }
        public List<string> Inputs { get; } = [];
        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            Inputs.AddRange(inputs);
            if (Calls == 1) throw new HttpRequestException("quota", null, HttpStatusCode.TooManyRequests);
            return Task.FromResult<IReadOnlyList<float[]>>([new float[] { 1, 2 }]);
        }
    }

    private sealed class FakeScheduler : IEmbeddingBatchScheduler
    {
        public int Retries { get; private set; }
        public Task WaitForDocumentBatchAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void MarkDocumentBatchCompleted() { }
        public Task WaitAfterRateLimitAsync(CancellationToken cancellationToken)
        {
            Retries++;
            return Task.CompletedTask;
        }
    }
}
