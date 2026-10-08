using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Domain.Organizations;

public sealed class OrganizationMembership : AuditableEntityBase
{
    private OrganizationMembership()
    {
    }

    private OrganizationMembership(Guid id, Guid organizationId, Guid userId, Guid invitedByUserId, DateTimeOffset now)
        : base(id, now)
    {
        OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId));
        UserId = DomainGuard.RequiredId(userId, nameof(userId));
        InvitedByUserId = DomainGuard.RequiredId(invitedByUserId, nameof(invitedByUserId));
        Status = OrganizationMembershipStatus.Invited;
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public OrganizationMembershipStatus Status { get; private set; }

    public DateTimeOffset? JoinedAt { get; private set; }

    public DateTimeOffset? RemovedAt { get; private set; }

    public Guid? RemovedByUserId { get; private set; }

    public Guid? InvitedByUserId { get; private set; }

    public Organization? Organization { get; private set; }

    public static OrganizationMembership Create(Guid organizationId, Guid userId, Guid invitedByUserId, DateTimeOffset now)
    {
        return new OrganizationMembership(Guid.NewGuid(), organizationId, userId, invitedByUserId, now);
    }

    public void Activate(DateTimeOffset joinedAt)
    {
        if (Status is not OrganizationMembershipStatus.Invited and not OrganizationMembershipStatus.Suspended)
        {
            throw new DomainException("organization_membership.invalid_activation", DomainMessages.OrganizationMembershipInvalidActivation);
        }

        var utcJoinedAt = DomainGuard.Utc(joinedAt, nameof(joinedAt));
        JoinedAt ??= utcJoinedAt;
        Status = OrganizationMembershipStatus.Active;
    }

    public void Suspend()
    {
        if (Status != OrganizationMembershipStatus.Active)
        {
            throw new DomainException("organization_membership.invalid_suspension", DomainMessages.OrganizationMembershipInvalidSuspension);
        }

        Status = OrganizationMembershipStatus.Suspended;
    }

    public void Rejoin(DateTimeOffset joinedAt)
    {
        if (Status != OrganizationMembershipStatus.Removed)
        {
            throw new DomainException("organization_membership.invalid_rejoin", DomainMessages.OrganizationMembershipInvalidActivation);
        }

        JoinedAt = DomainGuard.Utc(joinedAt, nameof(joinedAt));
        RemovedAt = null;
        RemovedByUserId = null;
        Status = OrganizationMembershipStatus.Active;
    }

    public void Remove(Guid removedByUserId, DateTimeOffset removedAt)
    {
        DomainGuard.RequiredId(removedByUserId, nameof(removedByUserId));

        if (Status == OrganizationMembershipStatus.Removed)
        {
            throw new DomainException("organization_membership.already_removed", DomainMessages.OrganizationMembershipAlreadyRemoved);
        }

        var utcRemovedAt = DomainGuard.Utc(removedAt, nameof(removedAt));
        if (JoinedAt is not null && utcRemovedAt < JoinedAt.Value)
        {
            throw new DomainException("organization_membership.invalid_removed_at", DomainMessages.OrganizationMembershipInvalidRemovedAt);
        }

        RemovedAt = utcRemovedAt;
        RemovedByUserId = removedByUserId;
        Status = OrganizationMembershipStatus.Removed;
    }
}
