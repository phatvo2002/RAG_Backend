using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Domain.Organizations;

public sealed class DepartmentMembership : EntityBase
{
    private DepartmentMembership()
    {
    }

    private DepartmentMembership(Guid id, Guid organizationId, Guid departmentId, Guid userId, DepartmentMembershipType membershipType, DateTimeOffset joinedAt)
        : base(id)
    {
        OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId));
        DepartmentId = DomainGuard.RequiredId(departmentId, nameof(departmentId));
        UserId = DomainGuard.RequiredId(userId, nameof(userId));
        MembershipType = membershipType;
        JoinedAt = DomainGuard.Utc(joinedAt, nameof(joinedAt));
    }

    public Guid OrganizationId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Guid UserId { get; private set; }

    public DepartmentMembershipType MembershipType { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public DateTimeOffset? LeftAt { get; private set; }

    public Guid? LeftByUserId { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public Organization? Organization { get; private set; }

    public Department? Department { get; private set; }

    public static DepartmentMembership Create(Guid organizationId, Guid departmentId, Guid userId, DepartmentMembershipType membershipType, DateTimeOffset joinedAt)
    {
        return new DepartmentMembership(Guid.NewGuid(), organizationId, departmentId, userId, membershipType, joinedAt);
    }

    public void Leave(Guid leftByUserId, DateTimeOffset leftAt)
    {
        DomainGuard.RequiredId(leftByUserId, nameof(leftByUserId));

        if (LeftAt is not null)
        {
            throw new DomainException("department_membership.already_left", DomainMessages.DepartmentMembershipAlreadyLeft);
        }

        var utcLeftAt = DomainGuard.Utc(leftAt, nameof(leftAt));
        if (utcLeftAt < JoinedAt)
        {
            throw new DomainException("department_membership.invalid_left_at", DomainMessages.DepartmentMembershipInvalidLeftAt);
        }

        LeftAt = utcLeftAt;
        LeftByUserId = leftByUserId;
    }
}
