using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Services;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations.Enums;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Authorization;

internal sealed class ApiPermissionService(NexoraDbContext dbContext, IUserContext userContext, ITenantContext tenantContext, IClock clock)
    : BaseService(userContext, tenantContext, clock), IPermissionChecker
{
    public async Task<bool> HasPermissionAsync(string permissionCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        if (!UserContext.IsAuthenticated ||
            !UserContext.UserId.HasValue ||
            !TenantContext.OrganizationId.HasValue)
        {
            return false;
        }

        var membershipIsActive = await dbContext.OrganizationMemberships
            .AsNoTracking()
            .AnyAsync(
                membership =>
                    membership.OrganizationId == CurrentOrganizationId &&
                    membership.UserId == CurrentUserId &&
                    membership.Status == OrganizationMembershipStatus.Active,
                cancellationToken);

        if (!membershipIsActive)
        {
            return false;
        }

        var now = UtcNow;
        var roleIds = await dbContext.OrganizationRoleAssignments
            .AsNoTracking()
            .Where(
                assignment =>
                    assignment.OrganizationId == CurrentOrganizationId &&
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

        var normalizedCode = permissionCode.Trim().ToUpperInvariant();
        var functionCode = ApiFunctionCode.Create(normalizedCode);
        var effects = await dbContext.RoleApiFunctions
            .AsNoTracking()
            .Where(
                grant =>
                    grant.OrganizationId == CurrentOrganizationId &&
                    roleIds.Contains(grant.RoleId) &&
                    grant.Function != null &&
                    grant.Function.Code == functionCode &&
                    grant.Function.Status == ApiFunctionStatus.Active &&
                    grant.RevokedAt == null &&
                    grant.ValidFrom <= now &&
                    (grant.ExpiresAt == null || now < grant.ExpiresAt))
            .Select(grant => grant.Effect)
            .ToListAsync(cancellationToken);

        return !effects.Contains(PermissionEffect.Deny) &&
               effects.Contains(PermissionEffect.Allow);
    }
}
