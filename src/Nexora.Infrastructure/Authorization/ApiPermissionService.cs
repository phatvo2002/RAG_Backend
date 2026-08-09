using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Permissions;
using Nexora.Application.Common.Services;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations.Enums;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Authorization;

internal sealed class ApiPermissionService(NexoraDbContext dbContext, IUserContext userContext, ITenantContext tenantContext, IDefaultOrganizationProvider organizationProvider, IClock clock)
    : BaseService(userContext, tenantContext, clock), IPermissionChecker, IApiRequestPermissionChecker
{
    public async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        if (!UserContext.IsAuthenticated || !UserContext.UserId.HasValue)
        {
            return false;
        }

        var functionCode = ApiFunctionCode.Create(permissionCode.Trim());
        var functionId = await dbContext.ApiFunctions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(function => function.Code == functionCode && function.Status == ApiFunctionStatus.Active)
            .Select(function => (Guid?)function.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return functionId.HasValue && await HasFunctionPermissionAsync(functionId.Value, cancellationToken);
    }

    public async Task<bool> HasAccessAsync(ApiRequestDescriptor request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!UserContext.IsAuthenticated || !UserContext.UserId.HasValue || !TryParseHttpMethod(request.HttpMethod, out var httpMethod))
        {
            return false;
        }

        var routeTemplate = NormalizeRouteTemplate(request.RouteTemplate);
        if (routeTemplate is null)
        {
            return false;
        }

        var canonicalRoute = RouteTemplate.Create(routeTemplate);
        var functionIds = await dbContext.ApiFunctions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(
                function =>
                    function.Status == ApiFunctionStatus.Active &&
                    (function.HttpMethod == httpMethod || function.HttpMethod == ApiHttpMethod.Any) &&
                    function.RouteTemplate == canonicalRoute)
            .Select(function => function.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (functionIds.Count != 1)
        {
            return false;
        }

        return await HasFunctionPermissionAsync(functionIds[0], cancellationToken);
    }

    private async Task<bool> HasFunctionPermissionAsync(Guid functionId, CancellationToken cancellationToken)
    {
        var organizationId = TenantContext.OrganizationId ?? await organizationProvider.GetOrganizationIdAsync(cancellationToken);
        if (!organizationId.HasValue)
        {
            return false;
        }

        var membershipIsActive = await dbContext.OrganizationMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.OrganizationId == organizationId.Value &&
                    membership.UserId == CurrentUserId &&
                    membership.Status == OrganizationMembershipStatus.Active,
                cancellationToken);

        if (!membershipIsActive)
        {
            return false;
        }

        var now = UtcNow;
        var roleIds = await dbContext.OrganizationRoleAssignments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(
                assignment =>
                    assignment.OrganizationId == organizationId.Value &&
                    assignment.UserId == CurrentUserId &&
                    assignment.Status == OrganizationRoleAssignmentStatus.Active &&
                    assignment.RevokedAt == null &&
                    assignment.ValidFrom <= now &&
                    (assignment.ExpiresAt == null || now < assignment.ExpiresAt))
            .Select(assignment => assignment.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            return false;
        }

        var effects = await dbContext.RoleApiFunctions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(
                grant =>
                    grant.OrganizationId == organizationId.Value &&
                    roleIds.Contains(grant.RoleId) &&
                    grant.FunctionId == functionId &&
                    grant.RevokedAt == null &&
                    grant.ValidFrom <= now &&
                    (grant.ExpiresAt == null || now < grant.ExpiresAt))
            .Select(grant => grant.Effect)
            .ToListAsync(cancellationToken);

        return !effects.Contains(PermissionEffect.Deny) &&
               effects.Contains(PermissionEffect.Allow);
    }

    private static bool TryParseHttpMethod(string value, out ApiHttpMethod httpMethod)
    {
        return Enum.TryParse(value.Trim(), ignoreCase: true, out httpMethod) && httpMethod != ApiHttpMethod.Any;
    }

    private static string? NormalizeRouteTemplate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var route = value.Trim();
        if (!route.StartsWith('/'))
        {
            route = $"/{route}";
        }

        route = route.TrimEnd('/');
        return route.Length == 0 || route.Length > 300 || route.Contains('?') || route.Contains('#') || route.Any(char.IsWhiteSpace)
            ? null
            : route;
    }
}
