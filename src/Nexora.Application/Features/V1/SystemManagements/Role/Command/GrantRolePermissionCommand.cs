using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

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
