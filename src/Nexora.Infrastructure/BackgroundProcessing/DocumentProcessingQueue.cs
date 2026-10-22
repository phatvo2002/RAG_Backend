using System.Threading.Channels;
using Nexora.Application.Common.Abstractions.Documents;

namespace Nexora.Infrastructure.BackgroundProcessing;

internal sealed class DocumentProcessingQueue : IDocumentProcessingQueue, IDocumentProcessingControl
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, CancellationTokenSource> active = new();

    public ValueTask EnqueueAsync(Guid versionId, CancellationToken cancellationToken) => _channel.Writer.WriteAsync(versionId, cancellationToken);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);

    public CancellationTokenSource Begin(Guid versionId, CancellationToken stoppingToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        if (!active.TryAdd(versionId, source))
        {
            source.Dispose();
            throw new InvalidOperationException("Document version is already processing.");
        }
        return source;
    }

    public bool Cancel(Guid versionId)
    {
        if (!active.TryGetValue(versionId, out var source)) return false;
        try { source.Cancel(); return true; }
        catch (ObjectDisposedException) { return false; }
    }

    public void End(Guid versionId)
    {
        if (active.TryRemove(versionId, out var source)) source.Dispose();
    }
}
