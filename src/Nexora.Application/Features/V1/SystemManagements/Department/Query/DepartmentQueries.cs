using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Permissions;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Department.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Department.Query;

public sealed record GetDepartmentsQuery : IQuery<IReadOnlyList<DepartmentResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.DepartmentRead;
}

public sealed class GetDepartmentsQueryHandler(IDepartmentRepository departmentRepository, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentResponse>>
{
    public async Task<IReadOnlyList<DepartmentResponse>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<DepartmentResponse>();
        }

        var departments = await departmentRepository.GetAllForOrganizationAsync(organizationId.Value, cancellationToken);
        return departments.Select(department => new DepartmentResponse(department.Id, department.ParentDepartmentId, department.Code.Value, department.Name, department.ManagerUserId, department.Status.ToString())).ToArray();
    }
}

public sealed record GetDepartmentQuery(Guid DepartmentId) : IQuery<Result<DepartmentResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.DepartmentReadById;
}

public sealed class GetDepartmentQueryHandler(IDepartmentRepository departmentRepository, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetDepartmentQuery, Result<DepartmentResponse>>
{
    public async Task<Result<DepartmentResponse>> Handle(GetDepartmentQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        var department = organizationId.HasValue ? await departmentRepository.GetByIdAsync(request.DepartmentId, asTracking: false, cancellationToken: cancellationToken) : null;
        return !organizationId.HasValue || department is null || department.OrganizationId != organizationId.Value
            ? Result<DepartmentResponse>.Failure(Error.NotFound(CommonMessages.Department.NotFound))
            : Result<DepartmentResponse>.Success(new DepartmentResponse(department.Id, department.ParentDepartmentId, department.Code.Value, department.Name, department.ManagerUserId, department.Status.ToString()));
    }
}

public sealed record GetDepartmentMembersQuery(Guid DepartmentId) : IQuery<IReadOnlyList<DepartmentMemberResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.DepartmentMemberRead;
}

public sealed class GetDepartmentMembersQueryHandler(IDepartmentMembershipRepository membershipRepository, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetDepartmentMembersQuery, IReadOnlyList<DepartmentMemberResponse>>
{
    public async Task<IReadOnlyList<DepartmentMemberResponse>> Handle(GetDepartmentMembersQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<DepartmentMemberResponse>();
        }

        var memberships = await membershipRepository.GetByDepartmentAsync(organizationId.Value, request.DepartmentId, cancellationToken);
        return memberships.Select(membership => new DepartmentMemberResponse(membership.UserId, membership.MembershipType.ToString(), membership.JoinedAt, membership.LeftAt)).ToArray();
    }
}
