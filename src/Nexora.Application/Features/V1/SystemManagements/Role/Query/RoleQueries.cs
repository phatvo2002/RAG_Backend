using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Permissions;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Query;

public sealed record GetRolesQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<RoleResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.RoleRead;
}

public sealed class GetRolesQueryHandler(IIdentityService identityService) : IRequestHandler<GetRolesQuery, PagedResult<RoleResponse>>
{
    public async Task<PagedResult<RoleResponse>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await identityService.GetRolesAsync(request.Page, request.PageSize, cancellationToken);
        return new PagedResult<RoleResponse>(roles.Items.Select(role => new RoleResponse(role.RoleId, role.Name, role.IsActive)).ToArray(), roles.Page, roles.PageSize, roles.TotalCount);
    }
}

public sealed record GetRoleQuery(Guid RoleId) : IQuery<Result<RoleResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.RoleReadById;
}

public sealed class GetRoleQueryHandler(IIdentityService identityService) : IRequestHandler<GetRoleQuery, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await identityService.GetRoleByIdAsync(request.RoleId, cancellationToken);
        return role is null ? Result<RoleResponse>.Failure(Error.NotFound(CommonMessages.Role.NotFound)) : Result<RoleResponse>.Success(new RoleResponse(role.RoleId, role.Name, role.IsActive));
    }
}

public sealed record GetRolePermissionsQuery(Guid RoleId) : IQuery<IReadOnlyList<RolePermissionResponse>>, IRequirePermission
{
    public string PermissionCode => PermissionCodes.RoleReadPermissions;
}

public sealed class GetRolePermissionsQueryHandler(IRoleApiFunctionRepository grantRepository, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<RolePermissionResponse>>
{
    public async Task<IReadOnlyList<RolePermissionResponse>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<RolePermissionResponse>();
        }

        var grants = await grantRepository.GetByRoleAsync(organizationId.Value, request.RoleId, cancellationToken);
        return grants.Select(grant => new RolePermissionResponse(grant.FunctionId, grant.Effect.ToString(), grant.ValidFrom, grant.ExpiresAt, grant.RevokedAt)).ToArray();
    }
}
