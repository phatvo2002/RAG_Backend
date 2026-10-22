using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Documents.Command;

public sealed record StopDocumentProcessingCommand(Guid DocumentId) : IRequest<Result>;
public sealed record RestartDocumentProcessingCommand(Guid DocumentId) : IRequest<Result>;

public sealed class StopDocumentProcessingCommandHandler(
    IUserContext userContext, IDefaultOrganizationProvider organizationProvider, IClock clock,
    IDocumentProcessingStore store, IDocumentProcessingControl control) : IRequestHandler<StopDocumentProcessingCommand, Result>
{
    public async Task<Result> Handle(StopDocumentProcessingCommand request, CancellationToken cancellationToken)
    {
        var access = await DocumentProcessingAccess.CheckAsync(request.DocumentId, userContext, organizationProvider, clock, store, cancellationToken);
        if (access.Error is not null) return Result.Failure(access.Error);
        var version = access.Version!;
        if (version.ProcessingStatus == "QUEUED")
        {
            if (await store.ChangeProcessingStatusAsync(version.Id, "QUEUED", "STOPPING", "STOPPING", cancellationToken))
            {
                if (!control.Cancel(version.Id))
                    await store.ChangeProcessingStatusAsync(version.Id, "STOPPING", "STOPPED", "STOPPED", cancellationToken);
                return Result.Success();
            }
        }
        else if (version.ProcessingStatus is "EXTRACTING" or "CHUNKING" or "INDEXING")
        {
            if (await store.ChangeProcessingStatusAsync(version.Id, version.ProcessingStatus, "STOPPING", "STOPPING", cancellationToken))
            {
                if (!control.Cancel(version.Id))
                    await store.ChangeProcessingStatusAsync(version.Id, "STOPPING", "STOPPED", "STOPPED", cancellationToken);
                return Result.Success();
            }
        }
        return Result.Failure(Error.Conflict("Trạng thái tài liệu đã thay đổi hoặc không thể dừng."));
    }
}

public sealed class RestartDocumentProcessingCommandHandler(
    IUserContext userContext, IDefaultOrganizationProvider organizationProvider, IClock clock,
    IDocumentProcessingStore store, IDocumentProcessingQueue queue, IObjectStorage objectStorage,
    IVectorStore vectorStore) : IRequestHandler<RestartDocumentProcessingCommand, Result>
{
    public async Task<Result> Handle(RestartDocumentProcessingCommand request, CancellationToken cancellationToken)
    {
        var access = await DocumentProcessingAccess.CheckAsync(request.DocumentId, userContext, organizationProvider, clock, store, cancellationToken);
        if (access.Error is not null) return Result.Failure(access.Error);
        var version = access.Version!;
        if (!await store.ChangeProcessingStatusAsync(version.Id, "STOPPED", "STOPPING", "STOPPING", cancellationToken))
            return Result.Failure(Error.Conflict("Chỉ có thể xử lý lại tài liệu đã dừng."));

        try
        {
            var vectors = await store.GetDocumentVectorPointsAsync(version.OrganizationId, request.DocumentId, cancellationToken);
            foreach (var group in vectors.GroupBy(item => item.CollectionName, StringComparer.Ordinal))
                await vectorStore.DeleteAsync(group.Key, group.Select(item => item.PointId).Distinct(StringComparer.Ordinal).ToArray(), cancellationToken);
            foreach (var item in await store.GetChunkStorageObjectsAsync(version.OrganizationId, request.DocumentId, cancellationToken))
                await objectStorage.DeleteAsync(item.Bucket, item.ObjectKey, cancellationToken);
            await store.DeleteDocumentChunksAsync(version.OrganizationId, request.DocumentId, cancellationToken);
            // DeleteDocumentChunksAsync stages EF deletes; persist them before a worker can see QUEUED.
            await store.SaveProcessingChangesAsync(cancellationToken);
            if (!await store.ChangeProcessingStatusAsync(version.Id, "STOPPING", "QUEUED", "PROCESSING", cancellationToken))
                return Result.Failure(Error.Conflict("Trạng thái tài liệu đã thay đổi."));
            await queue.EnqueueAsync(version.Id, cancellationToken);
            return Result.Success();
        }
        catch
        {
            await store.ChangeProcessingStatusAsync(version.Id, "STOPPING", "STOPPED", "STOPPED", CancellationToken.None);
            throw;
        }
    }
}

internal static class DocumentProcessingAccess
{
    public static async Task<(Nexora.Domain.Documents.DocumentVersion? Version, Error? Error)> CheckAsync(
        Guid documentId, IUserContext userContext, IDefaultOrganizationProvider organizationProvider,
        IClock clock, IDocumentProcessingStore store, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
            return (null, Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var document = await store.GetDocumentByIdAsync(documentId, false, cancellationToken);
        if (!organizationId.HasValue || document is null || document.OrganizationId != organizationId ||
            document.DeletedAt is not null || !document.FolderId.HasValue || !document.CurrentVersionId.HasValue)
            return (null, Error.NotFound("Tài liệu không tồn tại."));
        var folder = await store.GetFolderAsync(document.FolderId.Value, false, cancellationToken);
        if (folder is null || folder.OrganizationId != organizationId)
            return (null, Error.NotFound("Tài liệu không tồn tại."));
        if (!await store.CanUploadToFolderAsync(folder, userContext.UserId.Value, clock.UtcNow, cancellationToken))
            return (null, Error.Forbidden("Bạn không có quyền xử lý tài liệu trong thư mục này."));
        var version = await store.GetVersionAsync(document.CurrentVersionId.Value, false, cancellationToken);
        return version is null ? (null, Error.NotFound("Tài liệu không tồn tại.")) : (version, null);
    }
}
