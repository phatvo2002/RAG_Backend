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
using Nexora.Infrastructure.Identity;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record CreateRoleCommand(string Name) : ITransactionalCommand<Result<RoleResponse>>;

public sealed class CreateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        var existingRole = await unitOfWork.RoleRepository.GetAsync(
            role => role.NormalizedName == normalizedName,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        if (existingRole is not null)
        {
            return Result<RoleResponse>.Failure(Error.Conflict(CommonMessages.Role.CannotCreate));
        }

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = normalizedName,
            IsActive = true,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        unitOfWork.RoleRepository.Insert(role);
        return Result<RoleResponse>.Success(new RoleResponse(role.Id, role.Name, role.IsActive));
    }
}

public sealed record UpdateRoleCommand(Guid RoleId, string Name) : ITransactionalCommand<Result<RoleResponse>>;

public sealed class UpdateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateRoleCommand, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            cancellationToken: cancellationToken);
        if (role is null)
        {
            return Result<RoleResponse>.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        var name = request.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        var duplicateRole = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id != request.RoleId && item.NormalizedName == normalizedName,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        if (duplicateRole is not null)
        {
            return Result<RoleResponse>.Failure(Error.Conflict(CommonMessages.Role.CannotUpdate));
        }

        role.Name = name;
        role.NormalizedName = normalizedName;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        return Result<RoleResponse>.Success(new RoleResponse(role.Id, role.Name, role.IsActive));
    }
}

public sealed record DeactivateRoleCommand(Guid RoleId) : ITransactionalCommand<Result>;

public sealed class DeactivateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeactivateRoleCommand, Result>
{
    public async Task<Result> Handle(DeactivateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            cancellationToken: cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        role.IsActive = false;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Result.Success();
    }
}

public sealed record ReactivateRoleCommand(Guid RoleId) : ITransactionalCommand<Result>;

public sealed class ReactivateRoleCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ReactivateRoleCommand, Result>
{
    public async Task<Result> Handle(ReactivateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            cancellationToken: cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        role.IsActive = true;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
        return Result.Success();
    }
}

public sealed record GrantRolePermissionCommand(Guid RoleId, string FunctionCode, PermissionEffect Effect, DateTimeOffset? ValidFrom, DateTimeOffset? ExpiresAt) : ITransactionalCommand<Result>;

public sealed class GrantRolePermissionCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<GrantRolePermissionCommand, Result>
{
    public async Task<Result> Handle(GrantRolePermissionCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.Role.LoginRequiredToGrantPermission));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        var function = await unitOfWork.ApiFunctionRepository.GetByCodeAsync(request.FunctionCode, cancellationToken);
        if (!organizationId.HasValue || role is null || !role.IsActive || function is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.RoleOrganizationFunctionNotFound));
        }

        var existing = await unitOfWork.RoleApiFunctionRepository.GetAsync(organizationId.Value, request.RoleId, function.Id, asTracking: false, cancellationToken: cancellationToken);
        if (existing is not null && existing.RevokedAt is null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.Role.PermissionAlreadyGranted));
        }

        var validFrom = request.ValidFrom ?? clock.UtcNow;
        if (existing is not null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.Role.PermissionCannotBeGrantedAgain));
        }

        unitOfWork.RoleApiFunctionRepository.Insert(RoleApiFunction.Create(organizationId.Value, request.RoleId, function.Id, request.Effect, userContext.UserId.Value, validFrom, request.ExpiresAt));
        return Result.Success();
    }
}

public sealed record RevokeRolePermissionCommand(Guid RoleId, string FunctionCode) : ITransactionalCommand<Result>;

public sealed class RevokeRolePermissionCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<RevokeRolePermissionCommand, Result>
{
    public async Task<Result> Handle(RevokeRolePermissionCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.Role.LoginRequiredToRevokePermission));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var function = await unitOfWork.ApiFunctionRepository.GetByCodeAsync(request.FunctionCode, cancellationToken);
        if (!organizationId.HasValue || function is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.OrganizationFunctionNotFound));
        }

        var grant = await unitOfWork.RoleApiFunctionRepository.GetAsync(organizationId.Value, request.RoleId, function.Id, asTracking: true, cancellationToken: cancellationToken);
        if (grant is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Role.PermissionNotFound));
        }

        grant.Revoke(userContext.UserId.Value, clock.UtcNow);
        return Result.Success();
    }
}
