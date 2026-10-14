using System.Security.Cryptography;
using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Documents.Model;
using Nexora.Domain.Documents;

namespace Nexora.Application.Features.V1.Documents.Command;

public sealed record UploadDocumentCommand(Guid FolderId, string FileName, string ContentType, long SizeBytes, Stream Content) : IRequest<Result<DocumentUploadResponse>>;

public sealed class UploadDocumentCommandHandler(
    IUserContext userContext,
    IDefaultOrganizationProvider organizationProvider,
    IClock clock,
    IUnitOfWork unitOfWork,
    IDocumentProcessingStore store,
    IObjectStorage objectStorage,
    IDocumentProcessingQueue processingQueue,
    IDocumentStorageSettings storageSettings) : IRequestHandler<UploadDocumentCommand, Result<DocumentUploadResponse>>
{
    public async Task<Result<DocumentUploadResponse>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<DocumentUploadResponse>.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var userId = userContext.UserId.Value;
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
            return Result<DocumentUploadResponse>.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));

        var folder = await store.GetFolderAsync(request.FolderId, false, cancellationToken);
        if (folder is null || folder.OrganizationId != organizationId.Value)
            return Result<DocumentUploadResponse>.Failure(Error.NotFound("Không tìm thấy thư mục tài liệu."));

        var now = clock.UtcNow;
        if (!folder.DepartmentId.HasValue || !await store.CanUploadToFolderAsync(folder, userId, now, cancellationToken))
            return Result<DocumentUploadResponse>.Failure(Error.Forbidden("Bạn không có quyền tải tài liệu lên thư mục này."));

        var safeFileName = Path.GetFileName(request.FileName);
        var title = Path.GetFileNameWithoutExtension(safeFileName);
        await using var bufferedContent = new MemoryStream();
        await request.Content.CopyToAsync(bufferedContent, cancellationToken);
        var fileHash = SHA256.HashData(bufferedContent.ToArray());
        bufferedContent.Position = 0;

        var document = Document.Create(organizationId.Value, folder.DepartmentId.Value, userId, string.IsNullOrWhiteSpace(title) ? "Untitled" : title, now);
        document.SetFolder(folder.Id);
        document.SetStatus("PROCESSING");
        store.InsertDocument(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var versionNo = await store.GetNextVersionNoAsync(document.Id, cancellationToken);
        var objectKey = $"documents/{document.Id:D}/{versionNo:D}-{safeFileName}";
        var version = DocumentVersion.Create(organizationId.Value, document.Id, versionNo, safeFileName, request.ContentType, request.SizeBytes, "minio", storageSettings.Bucket, objectKey, fileHash, userId, now);
        store.InsertVersion(version);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        document.SetCurrentVersion(version.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await objectStorage.PutAsync(storageSettings.Bucket, objectKey, bufferedContent, request.SizeBytes, request.ContentType, cancellationToken);
            await processingQueue.EnqueueAsync(version.Id, cancellationToken);
            return Result<DocumentUploadResponse>.Success(new DocumentUploadResponse(document.Id, version.Id, version.ProcessingStatus));
        }
        catch
        {
            version.SetProcessingStatus("FAILED", "STORAGE_UPLOAD_FAILED");
            document.SetStatus("FAILED");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<DocumentUploadResponse>.Failure(new Error(CommonMessages.ErrorCodes.Server, CommonMessages.Api.UnexpectedError));
        }
    }
}
