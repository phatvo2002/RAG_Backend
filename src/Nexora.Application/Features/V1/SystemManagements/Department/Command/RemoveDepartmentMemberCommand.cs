using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.Department.Command;

public sealed record RemoveDepartmentMemberCommand(Guid DepartmentId, Guid UserId) : ITransactionalCommand<Result>;

public sealed class RemoveDepartmentMemberCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<RemoveDepartmentMemberCommand, Result>
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

        var membership = await unitOfWork.DepartmentMembershipRepository.GetAsync(organizationId.Value, request.DepartmentId, request.UserId, asTracking: true, cancellationToken: cancellationToken);
        if (membership is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Department.MembershipNotFound));
        }

        membership.Leave(userContext.UserId.Value, clock.UtcNow);
        return Result.Success();
    }
}
