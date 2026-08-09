using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Permissions;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Department.Model;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;
using DepartmentEntity = Nexora.Domain.Organizations.Department;

namespace Nexora.Application.Features.V1.SystemManagements.Department.Command;

public sealed record CreateDepartmentCommand(string Code, string Name, Guid? ParentDepartmentId, Guid? ManagerUserId) : ITransactionalCommand<Result<DepartmentResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.DepartmentCreate;
}

public sealed class CreateDepartmentCommandHandler(IDepartmentRepository departmentRepository, IDefaultOrganizationProvider organizationProvider, IIdentityService identityService, IClock clock) : IRequestHandler<CreateDepartmentCommand, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Result<DepartmentResponse>.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        }

        if (await departmentRepository.CodeExistsAsync(organizationId.Value, request.Code, null, cancellationToken))
        {
            return Result<DepartmentResponse>.Failure(Error.Conflict(CommonMessages.Department.CodeAlreadyExists));
        }

        if (request.ParentDepartmentId.HasValue && !await departmentRepository.ExistsAsync(request.ParentDepartmentId.Value, organizationId.Value, cancellationToken))
        {
            return Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.ParentNotFound));
        }

        if (request.ManagerUserId.HasValue)
        {
            var manager = await identityService.GetUserForManagementAsync(request.ManagerUserId.Value, cancellationToken);
            if (manager is null || !manager.IsActive)
            {
                return Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.ManagerNotFoundOrInactive));
            }
        }

        var department = DepartmentEntity.Create(organizationId.Value, request.Code, request.Name, clock.UtcNow, request.ParentDepartmentId, request.ManagerUserId);
        await departmentRepository.AddAsync(department, cancellationToken);
        return Result<DepartmentResponse>.Success(ToResponse(department));
    }

    private static DepartmentResponse ToResponse(DepartmentEntity department) => new(department.Id, department.ParentDepartmentId, department.Code.Value, department.Name, department.ManagerUserId, department.Status.ToString());
}

public sealed record UpdateDepartmentCommand(Guid DepartmentId, string Name, Guid? ParentDepartmentId, Guid? ManagerUserId) : ITransactionalCommand<Result<DepartmentResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.DepartmentUpdate;
}

public sealed class UpdateDepartmentCommandHandler(IDepartmentRepository departmentRepository, IDefaultOrganizationProvider organizationProvider, IIdentityService identityService) : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var department = organizationId.HasValue ? await departmentRepository.GetByIdAsync(request.DepartmentId, asTracking: true, cancellationToken: cancellationToken) : null;
        if (!organizationId.HasValue || department is null || department.OrganizationId != organizationId.Value)
        {
            return Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.NotFound));
        }

        if (request.ParentDepartmentId.HasValue && (request.ParentDepartmentId.Value == request.DepartmentId || !await departmentRepository.ExistsAsync(request.ParentDepartmentId.Value, organizationId.Value, cancellationToken)))
        {
            return Result<DepartmentResponse>.Failure(Error.Conflict(CommonMessages.Department.ParentInvalid));
        }

        if (request.ManagerUserId.HasValue)
        {
            var manager = await identityService.GetUserForManagementAsync(request.ManagerUserId.Value, cancellationToken);
            if (manager is null || !manager.IsActive)
            {
                return Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.ManagerNotFoundOrInactive));
            }
        }

        department.UpdateName(request.Name);
        department.UpdateParent(request.ParentDepartmentId);
        if (request.ManagerUserId.HasValue)
        {
            department.UpdateManager(request.ManagerUserId.Value);
        }
        else
        {
            department.RemoveManager();
        }

        return Result<DepartmentResponse>.Success(new DepartmentResponse(department.Id, department.ParentDepartmentId, department.Code.Value, department.Name, department.ManagerUserId, department.Status.ToString()));
    }
}

public sealed record ArchiveDepartmentCommand(Guid DepartmentId) : ITransactionalCommand<Result>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.DepartmentArchive;
}

public sealed class ArchiveDepartmentCommandHandler(IDepartmentRepository departmentRepository, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<ArchiveDepartmentCommand, Result>
{
    public async Task<Result> Handle(ArchiveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var department = organizationId.HasValue ? await departmentRepository.GetByIdAsync(request.DepartmentId, asTracking: true, cancellationToken: cancellationToken) : null;
        if (!organizationId.HasValue || department is null || department.OrganizationId != organizationId.Value)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Department.NotFound));
        }

        department.Archive();
        return Result.Success();
    }
}

public sealed record AddDepartmentMemberCommand(Guid DepartmentId, Guid UserId, DepartmentMembershipType MembershipType = DepartmentMembershipType.Member) : ITransactionalCommand<Result>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.DepartmentMemberAdd;
}

public sealed class AddDepartmentMemberCommandHandler(IDepartmentRepository departmentRepository, IDepartmentMembershipRepository membershipRepository, IIdentityService identityService, IDefaultOrganizationProvider organizationProvider, IClock clock) : IRequestHandler<AddDepartmentMemberCommand, Result>
{
    public async Task<Result> Handle(AddDepartmentMemberCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var department = organizationId.HasValue ? await departmentRepository.GetByIdAsync(request.DepartmentId, asTracking: false, cancellationToken: cancellationToken) : null;
        var user = await identityService.GetUserForManagementAsync(request.UserId, cancellationToken);
        if (!organizationId.HasValue || department is null || department.OrganizationId != organizationId.Value || user is null || !user.IsActive)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Department.DepartmentOrUserNotFound));
        }

        if (await membershipRepository.GetAsync(organizationId.Value, request.DepartmentId, request.UserId, asTracking: false, cancellationToken: cancellationToken) is not null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.Department.UserAlreadyMember));
        }

        await membershipRepository.AddAsync(DepartmentMembership.Create(organizationId.Value, request.DepartmentId, request.UserId, request.MembershipType, clock.UtcNow), cancellationToken);
        return Result.Success();
    }
}

public sealed record RemoveDepartmentMemberCommand(Guid DepartmentId, Guid UserId) : ITransactionalCommand<Result>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.DepartmentMemberRemove;
}

public sealed class RemoveDepartmentMemberCommandHandler(IDepartmentMembershipRepository membershipRepository, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<RemoveDepartmentMemberCommand, Result>
{
    public async Task<Result> Handle(RemoveDepartmentMemberCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.Department.LoginRequiredToRemoveMember));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Result.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        }

        var membership = await membershipRepository.GetAsync(organizationId.Value, request.DepartmentId, request.UserId, asTracking: true, cancellationToken: cancellationToken);
        if (membership is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Department.MembershipNotFound));
        }

        membership.Leave(userContext.UserId.Value, clock.UtcNow);
        return Result.Success();
    }
}
