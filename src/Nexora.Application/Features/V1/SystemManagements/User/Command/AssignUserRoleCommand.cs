using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.User.Command;

public sealed record AssignUserRoleCommand(Guid UserId, Guid RoleId) : ITransactionalCommand<Result>;

public sealed class AssignUserRoleCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock) : IRequestHandler<AssignUserRoleCommand, Result>
{
    public async Task<Result> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            return Result.Failure(Error.Unauthorized(CommonMessages.User.LoginRequiredToAssignRole));
        }

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var user = await unitOfWork.UserRepository.GetAsync(
            item => item.Id == request.UserId,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        if (!organizationId.HasValue || user is null || role is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.User.RoleOrOrganizationNotFound));
        }

        var existing = await unitOfWork.OrganizationRoleAssignmentRepository.GetAsync(organizationId.Value, request.UserId, request.RoleId, asTracking: true, cancellationToken: cancellationToken);
        if (existing?.Status == OrganizationRoleAssignmentStatus.Active)
        {
            return Result.Failure(Error.Conflict(CommonMessages.User.RoleAlreadyAssigned));
        }

        if (existing is not null)
        {
            unitOfWork.OrganizationRoleAssignmentRepository.Delete(existing);
        }

        unitOfWork.OrganizationRoleAssignmentRepository.Insert(OrganizationRoleAssignment.Create(organizationId.Value, request.UserId, request.RoleId, userContext.UserId.Value, clock.UtcNow));
        return Result.Success();
    }
}
