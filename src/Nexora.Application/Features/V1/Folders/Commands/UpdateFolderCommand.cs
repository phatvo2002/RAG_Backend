using FluentValidation;
using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Folders.Mappings;
using Nexora.Application.Features.V1.Folders.Model;

namespace Nexora.Application.Features.V1.Folders.Commands;

public sealed record UpdateFolderCommand(Guid FolderId, string Name)
    : ITransactionalCommand<Result<FolderDto>>;

public sealed class UpdateFolderCommandValidator : AbstractValidator<UpdateFolderCommand>
{
    public UpdateFolderCommandValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateFolderCommandHandler(
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDefaultOrganizationProvider organizationProvider) : IRequestHandler<UpdateFolderCommand, Result<FolderDto>>
{
    public async Task<Result<FolderDto>> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<FolderDto>.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var folder = organizationId.HasValue
            ? await unitOfWork.DocumentFolderRepository.GetByIdAsync(request.FolderId, asTracking: true, cancellationToken)
            : null;

        if (!organizationId.HasValue || folder is null || folder.OrganizationId != organizationId.Value)
        {
            return Result<FolderDto>.Failure(Error.NotFound("Folder không tồn tại."));
        }

        if (folder.OwnerUserId != userContext.UserId.Value)
        {
            return Result<FolderDto>.Failure(Error.Forbidden("Không có quyền quản lý folder."));
        }

        folder.Rename(request.Name);
        return Result<FolderDto>.Success(folder.ToDto());
    }
}
