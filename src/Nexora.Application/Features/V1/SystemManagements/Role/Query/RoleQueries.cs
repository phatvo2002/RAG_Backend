using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;
using Nexora.Application.Features.V1.SystemManagements.Role.Model;
using Nexora.Infrastructure.Identity;

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

public sealed record GetRoleQuery(Guid RoleId) : IQuery<Result<RoleDetailResponse>>;

public sealed class GetRoleQueryHandler(IUnitOfWork unitOfWork, IDefaultOrganizationProvider organizationProvider, IClock clock) : IRequestHandler<GetRoleQuery, Result<RoleDetailResponse>>
{
    public async Task<Result<RoleDetailResponse>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetAsync(
            item => item.Id == request.RoleId,
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return role is null
            ? Result<RoleDetailResponse>.Failure(Error.NotFound(CommonMessages.Role.NotFound))
            : await BuildRoleDetailAsync(role, request.RoleId, cancellationToken);
    }

    private async Task<Result<RoleDetailResponse>> BuildRoleDetailAsync(ApplicationRole role, Guid roleId, CancellationToken cancellationToken)
    {
        var apiFunctions = Array.Empty<ApiFunctionResponse>();
        var allFunctions = await unitOfWork.ApiFunctionRepository.GetAllAsync(
            orderBy: query => query.OrderBy(function => function.Module)
                .ThenBy(function => function.DisplayOrder)
                .ThenBy(function => function.Name),
            asNoTracking: true,
            cancellationToken: cancellationToken);
        var apiFunctionTree = ApiFunctionTreeBuilder.Build(allFunctions);
        var users = Array.Empty<RoleUserResponse>();
        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (organizationId.HasValue)
        {
            var now = clock.UtcNow;
            var roleUsers = await unitOfWork.UserRepository.GetByRoleAsync(organizationId.Value, roleId, now, cancellationToken);
            users = roleUsers.Select(user => new RoleUserResponse(user.Id, user.Email ?? user.UserName ?? string.Empty, user.UserName ?? user.Email ?? string.Empty, user.PhoneNumber, user.IsActive)).ToArray();

            var grants = await unitOfWork.RoleApiFunctionRepository.GetByRoleAsync(organizationId.Value, roleId, cancellationToken);
            var functionIds = grants
                .Where(grant => grant.RevokedAt is null && grant.ValidFrom <= now && (grant.ExpiresAt is null || grant.ExpiresAt > now))
                .Select(grant => grant.FunctionId)
                .Distinct()
                .ToArray();
            if (functionIds.Length > 0)
            {
                var functions = await unitOfWork.ApiFunctionRepository.GetActiveByIdsAsync(functionIds, cancellationToken);
                apiFunctions = functions
                    .OrderBy(function => function.Module)
                    .ThenBy(function => function.Name)
                    .Select(ApiFunctionResponse.FromEntity)
                    .ToArray();
            }
        }

        return Result<RoleDetailResponse>.Success(new RoleDetailResponse(
            role.Id,
            role.Name ?? string.Empty,
            role.IsActive,
            apiFunctions,
            users,
            apiFunctionTree));
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
