using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexora.Infrastructure.Rag;

namespace Nexora.Infrastructure.BackgroundProcessing;

internal sealed class DocumentProcessingWorker(IServiceScopeFactory scopeFactory, DocumentProcessingQueue queue, ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var versionId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<DocumentProcessingService>();
                await processor.ProcessAsync(versionId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Document processing worker failed for version {VersionId}.", versionId);
            }
        }
    }
}
