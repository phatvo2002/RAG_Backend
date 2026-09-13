using FluentValidation;
using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Folders.Mappings;
using Nexora.Application.Features.V1.Folders.Model;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Application.Features.V1.Folders.Commands;

public sealed record CreateFolderCommand(Guid DepartmentId, string Name, Guid? ParentFolderId)
    : ITransactionalCommand<Result<FolderDto>>;

public sealed class CreateFolderCommandValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderCommandValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateFolderCommandHandler(
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDefaultOrganizationProvider organizationProvider,
    IClock clock) : IRequestHandler<CreateFolderCommand, Result<FolderDto>>
{
    public async Task<Result<FolderDto>> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<FolderDto>.Failure(Error.Unauthorized(CommonMessages.Api.AuthenticationRequired));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Result<FolderDto>.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        }

        var department = await unitOfWork.DepartmentRepository.GetByIdAsync(
            request.DepartmentId,
            asTracking: false,
            cancellationToken);
        var membership = await unitOfWork.DepartmentMembershipRepository.GetAsync(
            organizationId.Value,
            request.DepartmentId,
            userContext.UserId.Value,
            asTracking: false,
            cancellationToken);

        if (department is null ||
            department.OrganizationId != organizationId.Value ||
            department.Status != DepartmentStatus.Active ||
            membership is null ||
            membership.LeftAt.HasValue)
        {
            return Result<FolderDto>.Failure(Error.Forbidden("Bạn không có quyền với department."));
        }

        if (request.ParentFolderId.HasValue &&
            !await unitOfWork.DocumentFolderRepository.ExistsInDepartmentAsync(
                organizationId.Value,
                request.DepartmentId,
                request.ParentFolderId.Value,
                cancellationToken))
        {
            return Result<FolderDto>.Failure(Error.Validation("Parent folder không hợp lệ."));
        }

        var folder = Domain.Documents.DocumentFolder.Create(
            organizationId.Value,
            request.DepartmentId,
            userContext.UserId.Value,
            request.Name,
            clock.UtcNow,
            request.ParentFolderId);

        unitOfWork.DocumentFolderRepository.Insert(folder);
        return Result<FolderDto>.Success(folder.ToDto());
    }
}
