namespace Nexora.Application.Common.Abstractions.Documents;

public interface IDocumentProcessingControl
{
    CancellationTokenSource Begin(Guid versionId, CancellationToken stoppingToken);
    bool Cancel(Guid versionId);
    void End(Guid versionId);
}
