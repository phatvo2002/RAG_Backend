using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Command;

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
