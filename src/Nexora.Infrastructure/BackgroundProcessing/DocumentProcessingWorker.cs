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
                await store.FinalizeStoppingAsync(stoppingToken);
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
            CancellationTokenSource? job = null;
            try
            {
                job = queue.Begin(versionId, stoppingToken);
                using var scope = scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IDocumentProcessingStore>();
                var version = await store.GetVersionAsync(versionId, false, job.Token);
                if (version is null || version.ProcessingStatus is "STOPPING" or "STOPPED" or "FAILED") continue;
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                using var logScope = logger.BeginScope(new Dictionary<string, object> { ["VersionId"] = versionId });
                await sender.Send(new ProcessDocumentVersionCommand(versionId), job.Token);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Document processing cancelled for version {VersionId}.", versionId);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Document processing worker failed for version {VersionId}.", versionId);
            }
            finally
            {
                queue.End(versionId);
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<IDocumentProcessingStore>();
                    await store.ChangeProcessingStatusAsync(versionId, "STOPPING", "STOPPED", "STOPPED", CancellationToken.None);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Could not finalize stopped version {VersionId}; startup recovery will retry.", versionId);
                }
            }
        }
    }
}
