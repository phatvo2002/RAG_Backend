using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.Folders.Commands;

public sealed record RevokeFolderAccessCommand(Guid FolderId, Guid GrantId) : ITransactionalCommand<Result>;

public sealed class RevokeFolderAccessCommandHandler(
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDefaultOrganizationProvider organizationProvider,
    IClock clock) : IRequestHandler<RevokeFolderAccessCommand, Result>
{
    public async Task<Result> Handle(RevokeFolderAccessCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var folder = organizationId.HasValue
            ? await unitOfWork.DocumentFolderRepository.GetByIdAsync(request.FolderId, asTracking: false, cancellationToken)
            : null;

        if (!organizationId.HasValue || folder is null || folder.OrganizationId != organizationId.Value)
        {
            return Result.Failure(Error.NotFound("Folder không tồn tại."));
        }

        if (folder.OwnerUserId != userContext.UserId.Value)
        {
            return Result.Failure(Error.Forbidden("Không có quyền thu hồi grant."));
        }

        var grant = await unitOfWork.DocumentFolderAccessGrantRepository.GetByIdAsync(
            organizationId.Value,
            request.FolderId,
            request.GrantId,
            asTracking: true,
            cancellationToken);
        if (grant is null)
        {
            return Result.Failure(Error.NotFound("Folder hoặc grant không tồn tại."));
        }

        grant.Revoke(userContext.UserId.Value, clock.UtcNow);
        return Result.Success();
    }
}
