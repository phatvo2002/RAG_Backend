using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Infrastructure.Services.DocumentProcessing;

namespace Nexora.Infrastructure.Services.Integrations.Embeddings;

internal sealed class EmbeddingRateLimiter(IOptions<DocumentProcessingOptions> options, TimeProvider timeProvider)
    : IEmbeddingRateLimiter, IEmbeddingBatchScheduler
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private const int MaxInputsPerWindow = 12;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Queue<DateTimeOffset> requests = new();
    private DateTimeOffset? lastDocumentBatchCompleted;

    public async Task WaitAsync(int inputCount, CancellationToken cancellationToken)
    {
        if (inputCount < 1 || inputCount > MaxInputsPerWindow)
            throw new ArgumentOutOfRangeException(nameof(inputCount));

        while (true)
        {
            TimeSpan wait;
            await gate.WaitAsync(cancellationToken);
            try
            {
                var now = timeProvider.GetUtcNow();
                while (requests.TryPeek(out var oldest) && now - oldest >= Window)
                    requests.Dequeue();
                if (requests.Count + inputCount <= MaxInputsPerWindow)
                {
                    for (var i = 0; i < inputCount; i++) requests.Enqueue(now);
                    return;
                }

                wait = Window - (now - requests.Peek());
            }
            finally
            {
                gate.Release();
            }

            await Task.Delay(wait, timeProvider, cancellationToken);
        }
    }

    public async Task WaitForDocumentBatchAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(0, options.Value.BatchIntervalSeconds));
        while (true)
        {
            TimeSpan wait;
            await gate.WaitAsync(cancellationToken);
            try
            {
                var now = timeProvider.GetUtcNow();
                wait = lastDocumentBatchCompleted.HasValue ? interval - (now - lastDocumentBatchCompleted.Value) : TimeSpan.Zero;
                if (wait <= TimeSpan.Zero)
                {
                    return;
                }
            }
            finally
            {
                gate.Release();
            }

            await Task.Delay(wait, timeProvider, cancellationToken);
        }
    }

    public void MarkDocumentBatchCompleted() => lastDocumentBatchCompleted = timeProvider.GetUtcNow();

    public Task WaitAfterRateLimitAsync(CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromSeconds(Math.Max(0, options.Value.RateLimitRetrySeconds)), timeProvider, cancellationToken);
}
