using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record CreateRoleCommand(string Name) : ITransactionalCommand<Result<RoleResponse>>;

public sealed class CreateRoleCommandHandler(IIdentityService identityService) : IRequestHandler<CreateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.CreateRoleAsync(request.Name, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Result<RoleResponse>.Success(new RoleResponse(result.Value.RoleId, result.Value.Name, result.Value.IsActive))
            : Result<RoleResponse>.Failure(result.Error ?? new Error(CommonMessages.ErrorCodes.IdentityOperationFailed, CommonMessages.Role.CannotCreate));
    }
}

public sealed record UpdateRoleCommand(Guid RoleId, string Name) : ITransactionalCommand<Result<RoleResponse>>;

public sealed class UpdateRoleCommandHandler(IIdentityService identityService) : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.UpdateRoleAsync(request.RoleId, request.Name, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? Result<RoleResponse>.Success(new RoleResponse(result.Value.RoleId, result.Value.Name, result.Value.IsActive))
            : Result<RoleResponse>.Failure(result.Error ?? new Error(CommonMessages.ErrorCodes.IdentityOperationFailed, CommonMessages.Role.CannotUpdate));
    }
}

public sealed record DeactivateRoleCommand(Guid RoleId) : ITransactionalCommand<Result>;

public sealed class DeactivateRoleCommandHandler(IIdentityService identityService) : IRequestHandler<DeactivateRoleCommand, Result>
{
    public Task<Result> Handle(DeactivateRoleCommand request, CancellationToken cancellationToken) => identityService.DeactivateRoleAsync(request.RoleId, cancellationToken);
}

public sealed record ReactivateRoleCommand(Guid RoleId) : ITransactionalCommand<Result>;

public sealed class ReactivateRoleCommandHandler(IIdentityService identityService) : IRequestHandler<ReactivateRoleCommand, Result>
{
    public Task<Result> Handle(ReactivateRoleCommand request, CancellationToken cancellationToken) => identityService.ReactivateRoleAsync(request.RoleId, cancellationToken);
}

public sealed record GrantRolePermissionCommand(Guid RoleId, string FunctionCode, PermissionEffect Effect, DateTimeOffset? ValidFrom, DateTimeOffset? ExpiresAt) : ITransactionalCommand<Result>;

public sealed class GrantRolePermissionCommandHandler(IIdentityService identityService, IApiFunctionRepository functionRepository, IRoleApiFunctionRepository grantRepository, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<GrantRolePermissionCommand, Result>
{
    public async Task<Result> Handle(GrantRolePermissionCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.Role.LoginRequiredToGrantPermission));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var role = await identityService.GetRoleByIdAsync(request.RoleId, cancellationToken);
        var function = await functionRepository.GetByCodeAsync(request.FunctionCode, cancellationToken);
        if (!organizationId.HasValue || role is null || !role.IsActive || function is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.RoleOrganizationFunctionNotFound));
        }

        var existing = await grantRepository.GetAsync(organizationId.Value, request.RoleId, function.Id, asTracking: false, cancellationToken: cancellationToken);
        if (existing is not null && existing.RevokedAt is null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.Role.PermissionAlreadyGranted));
        }

        var validFrom = request.ValidFrom ?? clock.UtcNow;
        if (existing is not null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.Role.PermissionCannotBeGrantedAgain));
        }

        await grantRepository.AddAsync(RoleApiFunction.Create(organizationId.Value, request.RoleId, function.Id, request.Effect, userContext.UserId.Value, validFrom, request.ExpiresAt), cancellationToken);
        return Result.Success();
    }
}

public sealed record RevokeRolePermissionCommand(Guid RoleId, string FunctionCode) : ITransactionalCommand<Result>;

public sealed class RevokeRolePermissionCommandHandler(IApiFunctionRepository functionRepository, IRoleApiFunctionRepository grantRepository, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<RevokeRolePermissionCommand, Result>
{
    public async Task<Result> Handle(RevokeRolePermissionCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.Role.LoginRequiredToRevokePermission));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var function = await functionRepository.GetByCodeAsync(request.FunctionCode, cancellationToken);
        if (!organizationId.HasValue || function is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.OrganizationFunctionNotFound));
        }

        var grant = await grantRepository.GetAsync(organizationId.Value, request.RoleId, function.Id, asTracking: true, cancellationToken: cancellationToken);
        if (grant is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.PermissionNotFound));
        }

        grant.Revoke(userContext.UserId.Value, clock.UtcNow);
        return Result.Success();
    }
}
