using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

public sealed record SyncRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<Guid> FunctionIds) : ITransactionalCommand<Result<RolePermissionsResponse>>;

public sealed class SyncRolePermissionsCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<SyncRolePermissionsCommand, Result<RolePermissionsResponse>>
{
    public async Task<Result<RolePermissionsResponse>> Handle(SyncRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result<RolePermissionsResponse>.Failure(Error.Unauthorized(CommonMessages.Role.LoginRequiredToGrantPermission));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Result<RolePermissionsResponse>.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        }

        var role = await unitOfWork.RoleRepository.GetAsync(role => role.Id == request.RoleId && role.IsActive, asNoTracking: true, cancellationToken: cancellationToken);
        if (role is null)
        {
            return Result<RolePermissionsResponse>.Failure(Error.NotFound(CommonMessages.Role.NotFound));
        }

        var functionIds = request.FunctionIds.Distinct().OrderBy(functionId => functionId).ToArray();
        var functions = await unitOfWork.ApiFunctionRepository.GetActiveByIdsAsync(functionIds, cancellationToken);
        if (functions.Count != functionIds.Length)
        {
            return Result<RolePermissionsResponse>.Failure(Error.NotFound(CommonMessages.Role.RoleOrganizationFunctionNotFound));
        }

        var now = clock.UtcNow;
        var actorUserId = userContext.UserId.Value;
        var grants = await unitOfWork.RoleApiFunctionRepository.GetByRoleAsync(organizationId.Value, request.RoleId, asTracking: true, cancellationToken);
        var requestedFunctionIds = functionIds.ToHashSet();
        var existingByFunctionId = grants.ToDictionary(grant => grant.FunctionId);

        foreach (var grant in grants)
        {
            if (!requestedFunctionIds.Contains(grant.FunctionId) && grant.RevokedAt is null)
            {
                grant.Revoke(actorUserId, grant.ValidFrom > now ? grant.ValidFrom : now);
            }
        }

        foreach (var functionId in functionIds)
        {
            if (!existingByFunctionId.TryGetValue(functionId, out var grant))
            {
                unitOfWork.RoleApiFunctionRepository.Insert(RoleApiFunction.Create(organizationId.Value, request.RoleId, functionId, PermissionEffect.Allow, actorUserId, now));
                continue;
            }

            if (grant.Effect != PermissionEffect.Allow || !grant.IsEffectiveAt(now))
            {
                grant.Regrant(PermissionEffect.Allow, actorUserId, now);
            }
        }

        return Result<RolePermissionsResponse>.Success(new RolePermissionsResponse(request.RoleId, functionIds));
    }
}
