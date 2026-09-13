using FluentValidation;
using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Folders.Mappings;
using Nexora.Application.Features.V1.Folders.Model;
using Nexora.Domain.Documents;

namespace Nexora.Application.Features.V1.Folders.Commands;

public sealed record GrantFolderAccessCommand(
    Guid FolderId,
    Guid? DepartmentId,
    Guid? UserId,
    string AccessLevel,
    DateTimeOffset? ExpiresAt) : ITransactionalCommand<Result<FolderGrantDto>>;

public sealed class GrantFolderAccessCommandValidator : AbstractValidator<GrantFolderAccessCommand>
{
    public GrantFolderAccessCommandValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty();
        RuleFor(x => x.AccessLevel).NotEmpty().MaximumLength(20);
        RuleFor(x => x)
            .Must(x => x.DepartmentId.HasValue ^ x.UserId.HasValue)
            .WithMessage("Phải chỉ định đúng một department hoặc user để cấp quyền.");
    }
}

public sealed class GrantFolderAccessCommandHandler(
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDefaultOrganizationProvider organizationProvider,
    IClock clock) : IRequestHandler<GrantFolderAccessCommand, Result<FolderGrantDto>>
{
    public async Task<Result<FolderGrantDto>> Handle(GrantFolderAccessCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<FolderGrantDto>.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var folder = organizationId.HasValue
            ? await unitOfWork.DocumentFolderRepository.GetByIdAsync(request.FolderId, asTracking: false, cancellationToken)
            : null;

        if (!organizationId.HasValue || folder is null || folder.OrganizationId != organizationId.Value)
        {
            return Result<FolderGrantDto>.Failure(Error.NotFound("Folder không tồn tại."));
        }

        if (folder.OwnerUserId != userContext.UserId.Value)
        {
            return Result<FolderGrantDto>.Failure(Error.Forbidden("Không có quyền cấp grant."));
        }

        try
        {
            var grant = DocumentFolderAccessGrant.Create(
                organizationId.Value,
                request.FolderId,
                request.DepartmentId,
                request.UserId,
                request.AccessLevel,
                userContext.UserId.Value,
                clock.UtcNow,
                request.ExpiresAt);

            unitOfWork.DocumentFolderAccessGrantRepository.Insert(grant);
            return Result<FolderGrantDto>.Success(grant.ToDto());
        }
        catch (ArgumentException exception)
        {
            return Result<FolderGrantDto>.Failure(Error.Validation(exception.Message));
        }
    }
}
