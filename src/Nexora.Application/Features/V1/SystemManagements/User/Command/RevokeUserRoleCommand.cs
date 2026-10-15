using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.User.Command;

public sealed record RevokeUserRoleCommand(Guid UserId, Guid RoleId) : ITransactionalCommand<Result>;

public sealed class RevokeUserRoleCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext) : IRequestHandler<RevokeUserRoleCommand, Result>
{
    public async Task<Result> Handle(RevokeUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.User.LoginRequiredToRevokeRole));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Result.Failure(Error.Conflict(CommonMessages.User.DefaultOrganizationNotConfigured));
        }

        var assignment = await unitOfWork.OrganizationRoleAssignmentRepository.GetAsync(organizationId.Value, request.UserId, request.RoleId, asTracking: true, cancellationToken: cancellationToken);
        if (assignment is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.RoleAssignmentNotFound));
        }

        unitOfWork.OrganizationRoleAssignmentRepository.Delete(assignment);
        return Result.Success();
    }
}
