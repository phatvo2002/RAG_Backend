using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.Department.Command;

public sealed record AddDepartmentMemberCommand(Guid DepartmentId, Guid UserId, DepartmentMembershipType MembershipType = DepartmentMembershipType.Member) : ITransactionalCommand<Result>;

public sealed class AddDepartmentMemberCommandHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IClock clock) : IRequestHandler<AddDepartmentMemberCommand, Result>
{
    public async Task<Result> Handle(AddDepartmentMemberCommand request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var department = organizationId.HasValue ? await unitOfWork.DepartmentRepository.GetByIdAsync(request.DepartmentId, asTracking: false, cancellationToken: cancellationToken) : null;
        var user = await unitOfWork.UserRepository.GetAsync(
            item => item.Id == request.UserId,
            asNoTracking: true,
            cancellationToken: cancellationToken);
        if (!organizationId.HasValue || department is null || department.OrganizationId != organizationId.Value || user is null || !user.IsActive)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Department.DepartmentOrUserNotFound));
        }

        if (await unitOfWork.DepartmentMembershipRepository.GetAsync(organizationId.Value, request.DepartmentId, request.UserId, asTracking: false, cancellationToken: cancellationToken) is not null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.Department.UserAlreadyMember));
        }

        unitOfWork.DepartmentMembershipRepository.Insert(DepartmentMembership.Create(organizationId.Value, request.DepartmentId, request.UserId, request.MembershipType, clock.UtcNow));
        return Result.Success();
    }
}
