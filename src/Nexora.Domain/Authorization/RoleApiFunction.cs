using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Organizations;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Domain.Authorization;

public sealed class RoleApiFunction : AuditableEntityBase
{
    private RoleApiFunction()
    {
    }

    private RoleApiFunction(
        Guid id, Guid organizationId, Guid roleId, Guid functionId, PermissionEffect effect,
        Guid grantedByUserId, DateTimeOffset validFrom, DateTimeOffset? expiresAt)
        : base(id, validFrom)
    {
        OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId));
        RoleId = DomainGuard.RequiredId(roleId, nameof(roleId));
        FunctionId = DomainGuard.RequiredId(functionId, nameof(functionId));
        Effect = effect;
        GrantedByUserId = DomainGuard.RequiredId(grantedByUserId, nameof(grantedByUserId));
        ValidFrom = DomainGuard.Utc(validFrom, nameof(validFrom));
        ExpiresAt = expiresAt is null ? null : DomainGuard.Utc(expiresAt.Value, nameof(expiresAt));
        ValidateValidityRange();
    }

    public Guid OrganizationId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid FunctionId { get; private set; }

    public PermissionEffect Effect { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public Guid GrantedByUserId { get; private set; }

    public Organization? Organization { get; private set; }

    public ApiFunction? Function { get; private set; }

    public static RoleApiFunction Create(
        Guid organizationId, Guid roleId, Guid functionId, PermissionEffect effect,
        Guid grantedByUserId, DateTimeOffset validFrom, DateTimeOffset? expiresAt = null)
    {
        return new RoleApiFunction(Guid.NewGuid(), organizationId, roleId, functionId, effect, grantedByUserId, validFrom, expiresAt);
    }

    public void Revoke(Guid revokedByUserId, DateTimeOffset revokedAt)
    {
        DomainGuard.RequiredId(revokedByUserId, nameof(revokedByUserId));

        if (RevokedAt is not null)
        {
            throw new DomainException("role_api_function.already_revoked", DomainMessages.RoleApiFunctionAlreadyRevoked);
        }

        var utcRevokedAt = DomainGuard.Utc(revokedAt, nameof(revokedAt));
        if (utcRevokedAt < ValidFrom)
        {
            throw new DomainException("role_api_function.invalid_revoked_at", DomainMessages.RoleApiFunctionInvalidRevokedAt);
        }

        RevokedAt = utcRevokedAt;
        RevokedByUserId = revokedByUserId;
    }

    public bool IsEffectiveAt(DateTimeOffset at)
    {
        var utcAt = DomainGuard.Utc(at, nameof(at));
        return RevokedAt is null &&
               utcAt >= ValidFrom &&
               (ExpiresAt is null || utcAt < ExpiresAt.Value);
    }

    private void ValidateValidityRange()
    {
        if (ExpiresAt is not null)
        {
            DomainGuard.EnsureBefore(ValidFrom, ExpiresAt.Value, "role_api_function.invalid_validity_range", DomainMessages.InvalidValidityRange);
        }
    }
}
