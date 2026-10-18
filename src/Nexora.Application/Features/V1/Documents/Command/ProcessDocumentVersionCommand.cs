using System.Net;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Domain.Documents;
using Nexora.Domain.Rag;

namespace Nexora.Application.Features.V1.Documents.Command;

public sealed record ProcessDocumentVersionCommand(Guid VersionId) : IRequest;

public sealed class ProcessDocumentVersionCommandHandler(
    IUnitOfWork unitOfWork,
    IDocumentProcessingStore store,
    IObjectStorage objectStorage,
    IDocumentTextExtractor textExtractor,
    IEmbeddingProvider embeddingProvider,
    IVectorStore vectorStore,
    IClock clock,
    IDocumentStorageSettings storageSettings,
    IDocumentIndexSettings indexSettings,
    IDocumentTextChunker chunker,
    IEmbeddingBatchScheduler batchScheduler,
    ILogger<ProcessDocumentVersionCommandHandler> logger) : IRequestHandler<ProcessDocumentVersionCommand>
{
    public async Task Handle(ProcessDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await store.GetVersionAsync(request.VersionId, true, cancellationToken);
        if (version is null || version.ProcessingStatus == "FAILED") return;
        var document = await store.GetDocumentByIdAsync(version.DocumentId, true, cancellationToken);
        if (document is null || document.DeletedAt != null) return;

        var reindexing = version.ProcessingStatus == "READY";
        try
        {
            var chunks = await store.GetChunksAsync(version.Id, true, cancellationToken);
            if (chunks.Count == 0)
            {
                version.SetProcessingStatus("EXTRACTING");
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await using var source = await objectStorage.GetAsync(version.StorageBucket, version.StorageObjectKey, cancellationToken);
                var extracted = await textExtractor.ExtractAsync(version.OriginalFileName, source, cancellationToken);
                var normalizedText = extracted.Text.Trim();
                if (string.IsNullOrWhiteSpace(normalizedText))
                    throw new InvalidOperationException("Không thể trích xuất nội dung văn bản từ tài liệu.");

                version.ExtractedContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedText))).ToLowerInvariant();
                version.ParserName = extracted.ParserName;
                version.ParserVersion = extracted.ParserVersion;
                version.SetProcessingStatus("CHUNKING");
                await unitOfWork.SaveChangesAsync(cancellationToken);
                foreach (var chunk in chunker.Chunk(normalizedText, indexSettings.ChunkSizeCharacters, indexSettings.ChunkOverlapCharacters))
                {
                    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chunk.Text))).ToLowerInvariant();
                    var entity = DocumentChunk.Create(version.OrganizationId, version.Id, chunk.Ordinal, hash, clock.UtcNow,
                        "vie", chunk.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, null, chunk.Start, chunk.End);
                    var objectKey = ChunkObjectKey(version.Id, chunk.Ordinal, hash);
                    await using var content = new MemoryStream(Encoding.UTF8.GetBytes(chunk.Text));
                    await objectStorage.PutAsync(storageSettings.Bucket, objectKey, content, content.Length, "text/plain; charset=utf-8", cancellationToken);
                    store.InsertChunk(entity);
                    store.InsertChunkContent(DocumentChunkContent.Create(entity.Id, "minio", storageSettings.Bucket, objectKey, hash, clock.UtcNow));
                    chunks.Add(entity);
                }
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (!reindexing)
            {
                version.SetProcessingStatus("INDEXING");
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var index = await store.GetIndexAsync(version.OrganizationId, indexSettings.IndexName, true, cancellationToken);
            if (index is null)
            {
                if (indexSettings.VectorDimension != indexSettings.EmbeddingDimension)
                    throw new InvalidOperationException("Kích thước vector phải trùng với kích thước embedding.");
                var config = $"{indexSettings.CollectionName}|{indexSettings.EmbeddingModel}|{indexSettings.EmbeddingDimension}|{indexSettings.Distance}";
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(config))).ToLowerInvariant();
                index = RagIndex.Create(version.OrganizationId, indexSettings.IndexName, "qdrant", indexSettings.CollectionName,
                    indexSettings.EmbeddingModel, indexSettings.EmbeddingDimension, indexSettings.Distance, hash, clock.UtcNow);
                index.Status = "BUILDING";
                store.InsertIndex(index);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            if (index.EmbeddingDimension != indexSettings.EmbeddingDimension ||
                index.CollectionName != indexSettings.CollectionName || index.EmbeddingModel != indexSettings.EmbeddingModel)
                throw new InvalidOperationException("Cấu hình embedding hiện tại không khớp RagIndex. Cần collection/index mới.");

            await vectorStore.EnsureCollectionAsync(index.CollectionName, index.EmbeddingDimension, index.DistanceMetric, cancellationToken);
            var pending = new List<DocumentChunk>();
            foreach (var chunk in chunks.Where(chunk => chunk.Status == "ACTIVE"))
            {
                var record = await store.GetIndexRecordAsync(index.Id, chunk.Id, false, cancellationToken);
                if (record?.Status != "INDEXED" || record.ContentHash != chunk.ContentHash) pending.Add(chunk);
            }

            var batchSize = Math.Clamp(indexSettings.BatchSize, 1, 5);
            for (var offset = 0; offset < pending.Count; offset += batchSize)
            {
                var batch = pending.Skip(offset).Take(batchSize).ToArray();
                var texts = new List<string>(batch.Length);
                foreach (var chunk in batch)
                {
                    await using var stream = await objectStorage.GetAsync(storageSettings.Bucket,
                        ChunkObjectKey(version.Id, chunk.Ordinal, chunk.ContentHash), cancellationToken);
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    texts.Add(await reader.ReadToEndAsync(cancellationToken));
                }

                IReadOnlyList<float[]> vectors;
                while (true)
                {
                    await batchScheduler.WaitForDocumentBatchAsync(cancellationToken);
                    try
                    {
                        vectors = await embeddingProvider.EmbedAsync(texts, cancellationToken);
                        batchScheduler.MarkDocumentBatchCompleted();
                        break;
                    }
                    catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        batchScheduler.MarkDocumentBatchCompleted();
                        version.FailureCode = "EMBEDDING_RATE_LIMITED";
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                        logger.LogWarning("Embedding quota reached for version {VersionId}; retrying.", version.Id);
                        await batchScheduler.WaitAfterRateLimitAsync(cancellationToken);
                    }
                }

                if (vectors.Count != batch.Length || vectors.Any(vector => vector.Length != index.EmbeddingDimension))
                    throw new InvalidOperationException("Embedding provider trả về số lượng hoặc kích thước vector không hợp lệ.");
                var points = batch.Select((chunk, i) => new VectorPoint(chunk.Id.ToString("D"), vectors[i], document.Id,
                    version.Id, chunk.Id, chunk.ContentHash, version.OriginalFileName)).ToArray();
                await vectorStore.UpsertAsync(index.CollectionName, points, cancellationToken);
                foreach (var point in points)
                {
                    var record = await store.GetIndexRecordAsync(index.Id, point.ChunkId, true, cancellationToken);
                    if (record is null)
                        store.InsertIndexRecord(RagIndexRecord.Create(version.OrganizationId, index.Id, point.ChunkId,
                            point.Id, point.ContentHash, clock.UtcNow));
                    else
                        record.MarkIndexed(point.ContentHash, clock.UtcNow);
                }
                version.FailureCode = null;
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (!reindexing)
            {
                version.SetProcessingStatus("READY", readyAt: clock.UtcNow);
                document.SetStatus("READY");
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            if (index.Status == "BUILDING" && await store.IsIndexCompleteAsync(version.OrganizationId, index.Id, cancellationToken))
            {
                index.Status = "ACTIVE";
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Document processing failed for version {VersionId}.", version.Id);
            if (reindexing) version.FailureCode = "REINDEX_FAILED";
            else
            {
                version.SetProcessingStatus("FAILED", "PROCESSING_FAILED");
                document.SetStatus("FAILED");
            }
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static string ChunkObjectKey(Guid versionId, int ordinal, string hash) =>
        $"chunks/{versionId:D}/{ordinal:D}-{hash}.txt";
}
