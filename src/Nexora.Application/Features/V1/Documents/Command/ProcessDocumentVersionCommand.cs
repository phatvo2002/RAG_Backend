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
    ILogger<ProcessDocumentVersionCommandHandler> logger) : IRequestHandler<ProcessDocumentVersionCommand>
{
    public async Task Handle(ProcessDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        var versionId = request.VersionId;
        var version = await store.GetVersionAsync(versionId, true, cancellationToken);
        if (version is null)
        {
            return;
        }

        var document = await store.GetDocumentByIdAsync(version.DocumentId, true, cancellationToken);
        if (document is null)
        {
            version.SetProcessingStatus("FAILED", "DOCUMENT_NOT_FOUND");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            version.SetProcessingStatus("EXTRACTING");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await using var source = await objectStorage.GetAsync(version.StorageBucket, version.StorageObjectKey, cancellationToken);
            var extracted = await textExtractor.ExtractAsync(version.OriginalFileName, source, cancellationToken);
            var normalizedText = extracted.Text.Trim();
            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                throw new InvalidOperationException("Không thể trích xuất nội dung văn bản từ tài liệu.");
            }

            version.ExtractedContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedText))).ToLowerInvariant();
            version.ParserName = extracted.ParserName;
            version.ParserVersion = extracted.ParserVersion;
            version.SetProcessingStatus("CHUNKING");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var chunks = chunker.Chunk(normalizedText, indexSettings.ChunkSizeCharacters, indexSettings.ChunkOverlapCharacters);
            var chunkEntities = new List<DocumentChunk>(chunks.Count);
            foreach (var chunk in chunks)
            {
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chunk.Text))).ToLowerInvariant();
                var entity = DocumentChunk.Create(version.OrganizationId, version.Id, chunk.Ordinal, hash, clock.UtcNow, "vie", chunk.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, null, chunk.Start, chunk.End);
                var objectKey = $"chunks/{version.Id:D}/{chunk.Ordinal:D}-{hash}.txt";
                await using var chunkContent = new MemoryStream(Encoding.UTF8.GetBytes(chunk.Text));
                await objectStorage.PutAsync(storageSettings.Bucket, objectKey, chunkContent, chunkContent.Length, "text/plain; charset=utf-8", cancellationToken);
                store.InsertChunk(entity);
                store.InsertChunkContent(DocumentChunkContent.Create(entity.Id, "minio", storageSettings.Bucket, objectKey, hash, clock.UtcNow));
                chunkEntities.Add(entity);
            }

            version.SetProcessingStatus("INDEXING");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var index = await store.GetIndexAsync(version.OrganizationId, "default", true, cancellationToken);
            if (index is null)
            {
                var dimension = indexSettings.EmbeddingDimension;
                if (indexSettings.VectorDimension != dimension)
                {
                    throw new InvalidOperationException("Qdrant:EmbeddingDimension phải trùng Embedding:Dimension.");
                }
                var configHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{indexSettings.CollectionName}|{indexSettings.EmbeddingModel}|{dimension}|{indexSettings.Distance}"))).ToLowerInvariant();
                index = RagIndex.Create(version.OrganizationId, "default", "qdrant", indexSettings.CollectionName, indexSettings.EmbeddingModel, dimension, indexSettings.Distance, configHash, clock.UtcNow);
                store.InsertIndex(index);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (index.EmbeddingDimension != indexSettings.EmbeddingDimension)
            {
                throw new InvalidOperationException("Kích thước vector của RagIndex không trùng Embedding:Dimension.");
            }

            await vectorStore.EnsureCollectionAsync(index.CollectionName, index.EmbeddingDimension, index.DistanceMetric, cancellationToken);
            const int batchSize = 32;
            for (var offset = 0; offset < chunkEntities.Count; offset += batchSize)
            {
                var batch = chunkEntities.Skip(offset).Take(batchSize).ToArray();
                var texts = new List<string>(batch.Length);
                foreach (var chunk in batch)
                {
                    await using var chunkStream = await objectStorage.GetAsync(storageSettings.Bucket, $"chunks/{version.Id:D}/{chunk.Ordinal:D}-{chunk.ContentHash}.txt", cancellationToken);
                    using var reader = new StreamReader(chunkStream, Encoding.UTF8);
                    texts.Add(await reader.ReadToEndAsync(cancellationToken));
                }

                var vectors = await embeddingProvider.EmbedAsync(texts, cancellationToken);
                var points = batch.Select((chunk, indexInBatch) => new VectorPoint(chunk.Id.ToString(), vectors[indexInBatch], document.Id, version.Id, chunk.Id, chunk.ContentHash, version.OriginalFileName)).ToArray();
                await vectorStore.UpsertAsync(index.CollectionName, points, cancellationToken);
                foreach (var point in points)
                {
                    var record = await store.GetIndexRecordAsync(index.Id, point.ChunkId, true, cancellationToken);
                    if (record is null)
                    {
                        store.InsertIndexRecord(RagIndexRecord.Create(version.OrganizationId, index.Id, point.ChunkId, point.Id, point.ContentHash, clock.UtcNow));
                    }
                    else
                    {
                        record.MarkIndexed(point.ContentHash, clock.UtcNow);
                    }
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            version.SetProcessingStatus("READY", readyAt: clock.UtcNow);
            document.SetStatus("READY");
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Document processing failed for version {VersionId}.", versionId);
            version.SetProcessingStatus("FAILED", "PROCESSING_FAILED");
            document.SetStatus("FAILED");
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
    }
}
