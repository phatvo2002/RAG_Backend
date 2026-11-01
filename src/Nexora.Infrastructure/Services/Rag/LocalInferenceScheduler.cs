using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Rag;

internal sealed class LocalInferenceScheduler : ILocalInferenceScheduler
{
    private readonly object sync = new();
    private readonly LinkedList<Waiter> chat = new();
    private readonly LinkedList<Waiter> embedding = new();
    private bool occupied;

    public Task<IDisposable> AcquireChatAsync(CancellationToken cancellationToken) => AcquireAsync(chat, cancellationToken);
    public Task<IDisposable> AcquireEmbeddingAsync(CancellationToken cancellationToken) => AcquireAsync(embedding, cancellationToken);

    private Task<IDisposable> AcquireAsync(LinkedList<Waiter> queue, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Waiter waiter;
        lock (sync)
        {
            if (!occupied && chat.Count == 0)
            {
                occupied = true;
                return Task.FromResult<IDisposable>(new Lease(this));
            }
            waiter = new Waiter();
            waiter.Node = queue.AddLast(waiter);
        }
        waiter.Registration = cancellationToken.Register(() => Cancel(waiter, cancellationToken));
        if (waiter.Task.IsCompleted) waiter.Registration.Dispose();
        return waiter.Task;
    }

    private void Cancel(Waiter waiter, CancellationToken cancellationToken)
    {
        lock (sync)
        {
            if (waiter.Node?.List is null) return;
            waiter.Node.List.Remove(waiter.Node);
            waiter.Node = null;
            waiter.Source.TrySetCanceled(cancellationToken);
        }
    }

    private void Release()
    {
        Waiter? next;
        lock (sync)
        {
            next = chat.First?.Value ?? embedding.First?.Value;
            if (next is null)
            {
                occupied = false;
                return;
            }
            next.Node!.List!.Remove(next.Node);
            next.Node = null;
            next.Source.TrySetResult(new Lease(this));
        }
        next.Registration.Dispose();
    }

    private sealed class Waiter
    {
        public TaskCompletionSource<IDisposable> Source { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IDisposable> Task => Source.Task;
        public LinkedListNode<Waiter>? Node { get; set; }
        public CancellationTokenRegistration Registration { get; set; }
    }

    private sealed class Lease(LocalInferenceScheduler owner) : IDisposable
    {
        private int disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0) owner.Release();
        }
    }
}
