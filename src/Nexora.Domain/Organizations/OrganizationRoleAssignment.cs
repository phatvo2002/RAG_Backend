using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Domain.Organizations;

public sealed class OrganizationRoleAssignment : AuditableEntityBase
{
    private OrganizationRoleAssignment()
    {
    }

    private OrganizationRoleAssignment(
        Guid id, Guid organizationId, Guid userId, Guid roleId, Guid assignedByUserId,
        DateTimeOffset validFrom, DateTimeOffset? expiresAt)
        : base(id, validFrom)
    {
        OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId));
        UserId = DomainGuard.RequiredId(userId, nameof(userId));
        RoleId = DomainGuard.RequiredId(roleId, nameof(roleId));
        AssignedByUserId = DomainGuard.RequiredId(assignedByUserId, nameof(assignedByUserId));
        ValidFrom = DomainGuard.Utc(validFrom, nameof(validFrom));
        ExpiresAt = expiresAt is null ? null : DomainGuard.Utc(expiresAt.Value, nameof(expiresAt));
        ValidateValidityRange();
        Status = OrganizationRoleAssignmentStatus.Active;
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public OrganizationRoleAssignmentStatus Status { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public Guid AssignedByUserId { get; private set; }

    public Organization? Organization { get; private set; }

    public static OrganizationRoleAssignment Create(
        Guid organizationId, Guid userId, Guid roleId, Guid assignedByUserId,
        DateTimeOffset validFrom, DateTimeOffset? expiresAt = null)
    {
        return new OrganizationRoleAssignment(Guid.NewGuid(), organizationId, userId, roleId, assignedByUserId, validFrom, expiresAt);
    }

    public void Revoke(Guid revokedByUserId, DateTimeOffset revokedAt)
    {
        DomainGuard.RequiredId(revokedByUserId, nameof(revokedByUserId));

        if (Status != OrganizationRoleAssignmentStatus.Active)
        {
            throw new DomainException("organization_role_assignment.not_active", DomainMessages.OrganizationRoleAssignmentNotActive);
        }

        var utcRevokedAt = DomainGuard.Utc(revokedAt, nameof(revokedAt));
        if (utcRevokedAt < ValidFrom)
        {
            throw new DomainException("organization_role_assignment.invalid_revoked_at", DomainMessages.OrganizationRoleAssignmentInvalidRevokedAt);
        }

        RevokedAt = utcRevokedAt;
        RevokedByUserId = revokedByUserId;
        Status = OrganizationRoleAssignmentStatus.Revoked;
    }

    public void Expire(DateTimeOffset now)
    {
        var utcNow = DomainGuard.Utc(now, nameof(now));

        if (Status != OrganizationRoleAssignmentStatus.Active || ExpiresAt is null || utcNow < ExpiresAt.Value)
        {
            throw new DomainException("organization_role_assignment.not_expired", DomainMessages.OrganizationRoleAssignmentNotExpired);
        }

        Status = OrganizationRoleAssignmentStatus.Expired;
    }

    public bool IsEffectiveAt(DateTimeOffset at)
    {
        var utcAt = DomainGuard.Utc(at, nameof(at));
        return Status == OrganizationRoleAssignmentStatus.Active &&
               utcAt >= ValidFrom &&
               (ExpiresAt is null || utcAt < ExpiresAt.Value);
    }

    private void ValidateValidityRange()
    {
        if (ExpiresAt is not null)
        {
            DomainGuard.EnsureBefore(ValidFrom, ExpiresAt.Value, "organization_role_assignment.invalid_validity_range", DomainMessages.InvalidValidityRange);
        }
    }
}
