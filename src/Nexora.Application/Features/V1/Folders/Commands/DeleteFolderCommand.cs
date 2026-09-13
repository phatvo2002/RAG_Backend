using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Folders.Commands;

public sealed record DeleteFolderCommand(Guid FolderId) : ITransactionalCommand<Result>;

public sealed class DeleteFolderCommandHandler(
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDefaultOrganizationProvider organizationProvider,
    IClock clock) : IRequestHandler<DeleteFolderCommand, Result>
{
    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var folder = organizationId.HasValue
            ? await unitOfWork.DocumentFolderRepository.GetByIdAsync(request.FolderId, asTracking: true, cancellationToken)
            : null;

        if (!organizationId.HasValue || folder is null || folder.OrganizationId != organizationId.Value)
        {
            return Result.Failure(Error.NotFound("Folder không tồn tại."));
        }

        if (folder.OwnerUserId != userContext.UserId.Value)
        {
            return Result.Failure(Error.Forbidden("Không có quyền xóa folder."));
        }

        if (await unitOfWork.DocumentFolderRepository.HasActiveDocumentsAsync(request.FolderId, cancellationToken))
        {
            return Result.Failure(Error.Conflict("Folder còn tài liệu."));
        }

        folder.MarkDeleted(userContext.UserId.Value, clock.UtcNow);
        return Result.Success();
    }
}
