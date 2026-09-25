using System.Threading.Channels;
using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.BackgroundProcessing;

internal sealed class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(Guid versionId, CancellationToken cancellationToken) => _channel.Writer.WriteAsync(versionId, cancellationToken);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}
