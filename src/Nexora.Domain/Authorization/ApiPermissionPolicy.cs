using Nexora.Domain.Authorization.Enums;

namespace Nexora.Domain.Authorization;

public static class ApiPermissionPolicy
{
    public static PermissionDecision Evaluate(IEnumerable<RoleApiFunction> grants, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(grants);

        var effectiveGrants = grants.Where(grant => grant.IsEffectiveAt(at));

        if (effectiveGrants.Any(grant => grant.Effect == PermissionEffect.Deny))
        {
            return PermissionDecision.Denied;
        }

        return effectiveGrants.Any(grant => grant.Effect == PermissionEffect.Allow)
            ? PermissionDecision.Allowed
            : PermissionDecision.NotGranted;
    }
}
