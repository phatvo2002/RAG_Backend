using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;
using Nexora.Domain.Documents;
using Nexora.Domain.Rag;
using Nexora.Infrastructure.Options;

namespace Nexora.Infrastructure.Rag;

internal sealed class DocumentIngestionService(
    IUnitOfWork unitOfWork,
    IDocumentProcessingStore store,
    IDefaultOrganizationProvider organizationProvider,
    IClock clock,
    IObjectStorage objectStorage,
    IDocumentProcessingQueue processingQueue,
    IOptions<StorageOptions> storageOptions) : IDocumentIngestionService
{
    public async Task<Result<DocumentUploadReceipt>> UploadAsync(Guid userId, Guid folderId, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Result<DocumentUploadReceipt>.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        }

        var folder = await store.GetFolderAsync(folderId, false, cancellationToken);
        if (folder is null || folder.OrganizationId != organizationId.Value)
        {
            return Result<DocumentUploadReceipt>.Failure(Error.NotFound("Không tìm thấy thư mục tài liệu."));
        }

        if (!folder.DepartmentId.HasValue || !await store.IsActiveDepartmentMemberAsync(folder.DepartmentId.Value, userId, cancellationToken))
        {
            return Result<DocumentUploadReceipt>.Failure(Error.Forbidden("Bạn không thuộc phòng ban của thư mục tài liệu."));
        }

        var safeFileName = Path.GetFileName(fileName);
        var title = Path.GetFileNameWithoutExtension(safeFileName);
        await using var bufferedContent = new MemoryStream();
        await content.CopyToAsync(bufferedContent, cancellationToken);
        var fileHash = SHA256.HashData(bufferedContent.ToArray());
        bufferedContent.Position = 0;

        var now = clock.UtcNow;
        var document = Document.Create(organizationId.Value, folder.DepartmentId.Value, userId, string.IsNullOrWhiteSpace(title) ? "Untitled" : title, now);
        document.SetFolder(folder.Id);
        var versionNo = await store.GetNextVersionNoAsync(document.Id, cancellationToken);
        var objectKey = $"documents/{document.Id:D}/{versionNo:D}-{safeFileName}";
        var version = DocumentVersion.Create(organizationId.Value, document.Id, versionNo, safeFileName, contentType, sizeBytes, "minio", storageOptions.Value.Bucket, objectKey, fileHash, userId, now);
        document.SetCurrentVersion(version.Id);
        document.SetStatus("PROCESSING");
        store.InsertDocument(document);
        store.InsertVersion(version);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await objectStorage.PutAsync(storageOptions.Value.Bucket, objectKey, bufferedContent, sizeBytes, contentType, cancellationToken);
            await processingQueue.EnqueueAsync(version.Id, cancellationToken);
            return Result<DocumentUploadReceipt>.Success(new DocumentUploadReceipt(document.Id, version.Id, version.ProcessingStatus));
        }
        catch
        {
            version.SetProcessingStatus("FAILED", "STORAGE_UPLOAD_FAILED");
            document.SetStatus("FAILED");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<DocumentUploadReceipt>.Failure(new Error(CommonMessages.ErrorCodes.Server, CommonMessages.Api.UnexpectedError));
        }
    }

    public async Task<DocumentStatusSnapshot?> GetStatusAsync(Guid userId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await store.GetDocumentAsync(userId, documentId, false, cancellationToken);
        if (document is null || !document.CurrentVersionId.HasValue)
        {
            return null;
        }

        var version = await store.GetVersionAsync(document.CurrentVersionId.Value, false, cancellationToken);
        if (version is null)
        {
            return null;
        }

        var chunks = await store.GetChunksAsync(version.Id, false, cancellationToken);
        return new DocumentStatusSnapshot(document.Id, version.Id, version.OriginalFileName, version.ProcessingStatus, version.FailureCode, version.CreatedAt, version.ReadyAt, chunks.Count);
    }
}

internal sealed class DocumentProcessingService(
    IUnitOfWork unitOfWork,
    IDocumentProcessingStore store,
    IObjectStorage objectStorage,
    IDocumentTextExtractor textExtractor,
    IEmbeddingProvider embeddingProvider,
    IVectorStore vectorStore,
    IClock clock,
    IOptions<StorageOptions> storageOptions,
    IOptions<QdrantOptions> qdrantOptions,
    IOptions<EmbeddingOptions> embeddingOptions,
    IOptions<DocumentProcessingOptions> processingOptions,
    ILogger<DocumentProcessingService> logger)
{
    public async Task ProcessAsync(Guid versionId, CancellationToken cancellationToken)
    {
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

            var chunks = ChunkText(normalizedText, processingOptions.Value.ChunkSizeCharacters, processingOptions.Value.ChunkOverlapCharacters);
            var chunkEntities = new List<DocumentChunk>(chunks.Count);
            foreach (var chunk in chunks)
            {
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chunk.Text))).ToLowerInvariant();
                var entity = DocumentChunk.Create(version.OrganizationId, version.Id, chunk.Ordinal, hash, clock.UtcNow, "vie", chunk.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, null, chunk.Start, chunk.End);
                var objectKey = $"chunks/{version.Id:D}/{chunk.Ordinal:D}-{hash}.txt";
                await using var chunkContent = new MemoryStream(Encoding.UTF8.GetBytes(chunk.Text));
                await objectStorage.PutAsync(storageOptions.Value.Bucket, objectKey, chunkContent, chunkContent.Length, "text/plain; charset=utf-8", cancellationToken);
                store.InsertChunk(entity);
                store.InsertChunkContent(DocumentChunkContent.Create(entity.Id, "minio", storageOptions.Value.Bucket, objectKey, hash, clock.UtcNow));
                chunkEntities.Add(entity);
            }

            version.SetProcessingStatus("INDEXING");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var index = await store.GetIndexAsync(version.OrganizationId, "default", true, cancellationToken);
            if (index is null)
            {
                var dimension = embeddingOptions.Value.Dimension;
                if (qdrantOptions.Value.EmbeddingDimension != dimension)
                {
                    throw new InvalidOperationException("Qdrant:EmbeddingDimension phải trùng Embedding:Dimension.");
                }
                var configHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{qdrantOptions.Value.CollectionName}|{embeddingOptions.Value.Model}|{dimension}|{qdrantOptions.Value.Distance}"))).ToLowerInvariant();
                index = RagIndex.Create(version.OrganizationId, "default", "qdrant", qdrantOptions.Value.CollectionName, embeddingOptions.Value.Model, dimension, qdrantOptions.Value.Distance, configHash, clock.UtcNow);
                store.InsertIndex(index);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (index.EmbeddingDimension != embeddingOptions.Value.Dimension)
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
                    await using var chunkStream = await objectStorage.GetAsync(storageOptions.Value.Bucket, $"chunks/{version.Id:D}/{chunk.Ordinal:D}-{chunk.ContentHash}.txt", cancellationToken);
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

    private static List<TextChunk> ChunkText(string text, int chunkSize, int overlap)
    {
        if (chunkSize < 100 || overlap < 0 || overlap >= chunkSize)
        {
            throw new InvalidOperationException("DocumentProcessing chunk configuration không hợp lệ.");
        }

        var result = new List<TextChunk>();
        for (var start = 0; start < text.Length;)
        {
            var length = Math.Min(chunkSize, text.Length - start);
            var end = start + length;
            if (end < text.Length)
            {
                var boundary = text.LastIndexOfAny([' ', '\n', '\r', '\t'], end - 1, Math.Min(200, length));
                if (boundary > start + (chunkSize / 2))
                {
                    end = boundary;
                }
            }

            var value = text[start..end].Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Add(new TextChunk(result.Count, value, start, end));
            }

            if (end >= text.Length)
            {
                break;
            }

            start = Math.Max(start + 1, end - overlap);
        }

        return result;
    }

    private sealed record TextChunk(int Ordinal, string Text, int Start, int End);
}
