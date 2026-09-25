using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;
using Nexora.Domain.Documents;
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

        var now = clock.UtcNow;
        if (!folder.DepartmentId.HasValue || !await store.CanUploadToFolderAsync(folder, userId, now, cancellationToken))
        {
            return Result<DocumentUploadReceipt>.Failure(Error.Forbidden("Bạn không có quyền tải tài liệu lên thư mục này."));
        }

        var safeFileName = Path.GetFileName(fileName);
        var title = Path.GetFileNameWithoutExtension(safeFileName);
        await using var bufferedContent = new MemoryStream();
        await content.CopyToAsync(bufferedContent, cancellationToken);
        var fileHash = SHA256.HashData(bufferedContent.ToArray());
        bufferedContent.Position = 0;

        var document = Document.Create(organizationId.Value, folder.DepartmentId.Value, userId, string.IsNullOrWhiteSpace(title) ? "Untitled" : title, now);
        document.SetFolder(folder.Id);
        document.SetStatus("PROCESSING");
        store.InsertDocument(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var versionNo = await store.GetNextVersionNoAsync(document.Id, cancellationToken);
        var objectKey = $"documents/{document.Id:D}/{versionNo:D}-{safeFileName}";
        var version = DocumentVersion.Create(organizationId.Value, document.Id, versionNo, safeFileName, contentType, sizeBytes, "minio", storageOptions.Value.Bucket, objectKey, fileHash, userId, now);
        store.InsertVersion(version);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        document.SetCurrentVersion(version.Id);
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
