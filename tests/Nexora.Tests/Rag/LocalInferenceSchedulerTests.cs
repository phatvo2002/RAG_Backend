using Nexora.Infrastructure.Services.Rag;

namespace Nexora.Tests.Rag;

public sealed class LocalInferenceSchedulerTests
{
    [Fact]
    public async Task Chat_runs_before_next_document_batch()
    {
        var scheduler = new LocalInferenceScheduler();
        using var firstBatch = await scheduler.AcquireEmbeddingAsync(default);
        var nextBatch = scheduler.AcquireEmbeddingAsync(default);
        var chat = scheduler.AcquireChatAsync(default);
        Assert.False(nextBatch.IsCompleted);
        Assert.False(chat.IsCompleted);

        firstBatch.Dispose();
        using var chatLease = await chat.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(nextBatch.IsCompleted);
        chatLease.Dispose();
        using var next = await nextBatch.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Cancelled_waiter_does_not_block_next_request()
    {
        var scheduler = new LocalInferenceScheduler();
        using var first = await scheduler.AcquireEmbeddingAsync(default);
        using var cts = new CancellationTokenSource();
        var waiting = scheduler.AcquireEmbeddingAsync(cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        first.Dispose();
        using var next = await scheduler.AcquireChatAsync(default).WaitAsync(TimeSpan.FromSeconds(2));
    }
}
