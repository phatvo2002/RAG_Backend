using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MediatR;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Features.V1.Documents.Command;

namespace Nexora.Infrastructure.BackgroundProcessing;

internal sealed class DocumentProcessingWorker(IServiceScopeFactory scopeFactory, DocumentProcessingQueue queue, ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IDocumentProcessingStore>();
                var settings = scope.ServiceProvider.GetRequiredService<IDocumentIndexSettings>();
                foreach (var versionId in await store.GetPendingVersionIdsAsync(settings.IndexName, stoppingToken))
                    await queue.EnqueueAsync(versionId, stoppingToken);
                break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not restore pending document processing; retrying in 30 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        await foreach (var versionId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new ProcessDocumentVersionCommand(versionId), stoppingToken);
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
