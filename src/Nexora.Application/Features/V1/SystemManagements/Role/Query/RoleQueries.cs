using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;

namespace Nexora.Application.Features.V1.SystemManagements.Role.Query;

public sealed record GetRolesQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<RoleResponse>>;

public sealed class GetRolesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetRolesQuery, PagedResult<RoleResponse>>
{
    public async Task<PagedResult<RoleResponse>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var roles = await unitOfWork.RoleRepository.PaginationAsync(
            page,
            pageSize,
            orderBy: query => query.OrderBy(role => role.Name),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return new PagedResult<RoleResponse>(
            roles.Data.Select(role => new RoleResponse(role.Id, role.Name ?? string.Empty, role.IsActive)).ToArray(),
            page,
            pageSize,
            roles.Total);
    }
}

public sealed record GetRoleQuery(Guid RoleId) : IQuery<Result<RoleResponse>>;

public sealed class GetRoleQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetRoleQuery, Result<RoleResponse>>
{
    public async Task<Result<RoleResponse>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return role is null
            ? Result<RoleResponse>.Failure(Error.NotFound(CommonMessages.Role.NotFound))
            : Result<RoleResponse>.Success(new RoleResponse(role.Id, role.Name ?? string.Empty, role.IsActive));
    }
}

public sealed record GetRolePermissionsQuery(Guid RoleId) : IQuery<IReadOnlyList<RolePermissionResponse>>;

public sealed class GetRolePermissionsQueryHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider) : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<RolePermissionResponse>>
{
    public async Task<IReadOnlyList<RolePermissionResponse>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return Array.Empty<RolePermissionResponse>();
        }

        var grants = await unitOfWork.RoleApiFunctionRepository.GetByRoleAsync(organizationId.Value, request.RoleId, cancellationToken);
        return grants.Select(grant => new RolePermissionResponse(grant.FunctionId, grant.Effect.ToString(), grant.ValidFrom, grant.ExpiresAt, grant.RevokedAt)).ToArray();
    }
}
