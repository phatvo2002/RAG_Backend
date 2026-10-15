namespace Nexora.Application.Common.Abstractions.Documents;

public interface IDocumentProcessingQueue
{
    ValueTask EnqueueAsync(Guid versionId, CancellationToken cancellationToken);
}
