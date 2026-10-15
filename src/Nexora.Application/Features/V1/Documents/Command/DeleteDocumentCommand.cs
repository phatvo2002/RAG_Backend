using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Documents.Command;

public sealed record DeleteDocumentCommand(Guid DocumentId) : IRequest<Result>;

public sealed class DeleteDocumentCommandHandler(
    IUserContext userContext,
    IDefaultOrganizationProvider organizationProvider,
    IClock clock,
    IUnitOfWork unitOfWork,
    IDocumentProcessingStore store,
    IObjectStorage objectStorage,
    IVectorStore vectorStore) : IRequestHandler<DeleteDocumentCommand, Result>
{
    public async Task<Result> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
            return Result.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));

        var userId = userContext.UserId.Value;
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
            return Result.Failure(Error.NotFound("Tài liệu không tồn tại."));

        var document = await store.GetDocumentByIdAsync(request.DocumentId, true, cancellationToken);
        if (document is null || document.OrganizationId != organizationId.Value || !document.FolderId.HasValue)
            return Result.Failure(Error.NotFound("Tài liệu không tồn tại."));

        var folder = await store.GetFolderAsync(document.FolderId.Value, false, cancellationToken);
        if (folder is null || folder.OrganizationId != organizationId.Value)
            return Result.Failure(Error.NotFound("Tài liệu không tồn tại."));

        if (!await store.CanUploadToFolderAsync(folder, userId, clock.UtcNow, cancellationToken))
            return Result.Failure(Error.Forbidden("Bạn không có quyền xóa tài liệu trong thư mục này."));

        var versions = await store.GetDocumentVersionsAsync(organizationId.Value, request.DocumentId, cancellationToken);
        if (versions.Any(version => !IsTerminal(version.ProcessingStatus)))
            return Result.Failure(Error.Conflict("Không thể xóa tài liệu khi đang xử lý. Vui lòng thử lại sau."));

        var vectorPoints = await store.GetDocumentVectorPointsAsync(organizationId.Value, request.DocumentId, cancellationToken);
        foreach (var collection in vectorPoints.GroupBy(point => point.CollectionName, StringComparer.Ordinal))
            await vectorStore.DeleteAsync(collection.Key, collection.Select(point => point.PointId).Distinct(StringComparer.Ordinal).ToArray(), cancellationToken);

        var storedObjects = versions
            .Select(version => new StoredObjectReference(version.StorageProvider, version.StorageBucket, version.StorageObjectKey))
            .Concat(await store.GetChunkStorageObjectsAsync(organizationId.Value, request.DocumentId, cancellationToken))
            .Distinct().ToArray();
        foreach (var storedObject in storedObjects)
        {
            if (!string.Equals(storedObject.StorageProvider, "minio", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Không hỗ trợ xóa tài liệu từ storage provider '{storedObject.StorageProvider}'.");
            await objectStorage.DeleteAsync(storedObject.Bucket, storedObject.ObjectKey, cancellationToken);
        }

        await store.MarkDocumentVectorRecordsDeletedAsync(organizationId.Value, request.DocumentId, cancellationToken);
        if (document.DeletedAt is null)
            document.MarkDeleted(userId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool IsTerminal(string status) =>
        string.Equals(status, "READY", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "FAILED", StringComparison.OrdinalIgnoreCase);
}
