using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Tests.Domain;

public sealed class OrganizationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidValues_ShouldNormalizeSlugAndSetActiveStatus()
    {
        var organization = Organization.Create(" Acme-Vietnam ", "Acme Vietnam", Now);

        Assert.Equal("acme-vietnam", organization.Slug.Value);
        Assert.Equal("Acme Vietnam", organization.Name);
        Assert.Equal(OrganizationStatus.Active, organization.Status);
    }

    [Fact]
    public void Archive_ThenUpdateName_ShouldThrowDomainException()
    {
        var organization = Organization.Create("acme-vietnam", "Acme Vietnam", Now);

        organization.Archive();

        var exception = Assert.Throws<DomainException>(() => organization.UpdateName("Renamed"));

        Assert.Equal("organization.archived", exception.Code);
    }

    [Fact]
    public void Suspend_ThenActivate_ShouldRestoreActiveStatus()
    {
        var organization = Organization.Create("acme-vietnam", "Acme Vietnam", Now);

        organization.Suspend();
        organization.Activate();

        Assert.Equal(OrganizationStatus.Active, organization.Status);
    }

    [Fact]
    public void Department_UpdateParentToItself_ShouldThrowDomainException()
    {
        var department = Department.Create(Guid.NewGuid(), "ENG", "Engineering", Now);

        var exception = Assert.Throws<DomainException>(() => department.UpdateParent(department.Id));

        Assert.Equal("department.self_parent", exception.Code);
    }

    [Fact]
    public void OrganizationMembership_RemoveBeforeJoinedAt_ShouldThrowDomainException()
    {
        var membership = OrganizationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        membership.Activate(Now.AddHours(1));

        var exception = Assert.Throws<DomainException>(() =>
            membership.Remove(Guid.NewGuid(), Now));

        Assert.Equal("organization_membership.invalid_removed_at", exception.Code);
    }

    [Fact]
    public void OrganizationMembership_RejoinAfterRemoval_ShouldRestoreActiveMembership()
    {
        var membership = OrganizationMembership.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        membership.Activate(Now);
        membership.Remove(Guid.NewGuid(), Now.AddDays(1));

        membership.Rejoin(Now.AddDays(2));

        Assert.Equal(OrganizationMembershipStatus.Active, membership.Status);
        Assert.Equal(Now.AddDays(2), membership.JoinedAt);
        Assert.Null(membership.RemovedAt);
        Assert.Null(membership.RemovedByUserId);
    }
}
