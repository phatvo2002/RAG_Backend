using Nexora.Domain.Authorization;
using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Domain.Organizations;

public sealed class Organization : SoftDeletableEntityBase
{
    private Organization()
    {
    }

    private Organization(Guid id, OrganizationSlug slug, string name, string? dataResidency, DateTimeOffset now)
        : base(id, now)
    {
        Slug = slug;
        Name = DomainGuard.RequiredText(name, nameof(name), 200);
        DataResidency = dataResidency is null
            ? null
            : DomainGuard.RequiredText(dataResidency, nameof(dataResidency), 50);
        Status = OrganizationStatus.Active;
    }

    public OrganizationSlug Slug { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public OrganizationStatus Status { get; private set; }

    public string? DataResidency { get; private set; }

    private readonly List<OrganizationMembership> _memberships = new();

    public IReadOnlyCollection<OrganizationMembership> Memberships => _memberships;

    private readonly List<OrganizationRoleAssignment> _roleAssignments = new();

    public IReadOnlyCollection<OrganizationRoleAssignment> RoleAssignments => _roleAssignments;

    public OrganizationSetting? Settings { get; private set; }

    private readonly List<Department> _departments = new();

    public IReadOnlyCollection<Department> Departments => _departments;

    private readonly List<RoleApiFunction> _roleApiFunctions = new();

    public IReadOnlyCollection<RoleApiFunction> RoleApiFunctions => _roleApiFunctions;

    public static Organization Create(string slug, string name, DateTimeOffset now, string? dataResidency = null)
    {
        return new Organization(Guid.NewGuid(), OrganizationSlug.Create(slug), name, dataResidency, now);
    }

    public void UpdateName(string name)
    {
        EnsureNotArchived();
        Name = DomainGuard.RequiredText(name, nameof(name), 200);
    }

    public void Suspend()
    {
        if (Status == OrganizationStatus.Archived)
        {
            throw new DomainException("organization.archived", DomainMessages.OrganizationArchivedCannotSuspend);
        }

        if (Status == OrganizationStatus.Suspended)
        {
            throw new DomainException("organization.already_suspended", DomainMessages.OrganizationAlreadySuspended);
        }

        Status = OrganizationStatus.Suspended;
    }

    public void Activate()
    {
        if (Status != OrganizationStatus.Suspended)
        {
            throw new DomainException("organization.invalid_activation", DomainMessages.OrganizationInvalidActivation);
        }

        Status = OrganizationStatus.Active;
    }

    public void Archive()
    {
        if (Status == OrganizationStatus.Archived)
        {
            throw new DomainException("organization.already_archived", DomainMessages.OrganizationAlreadyArchived);
        }

        Status = OrganizationStatus.Archived;
    }

    public void UpdateDataResidency(string? dataResidency)
    {
        EnsureNotArchived();
        DataResidency = dataResidency is null
            ? null
            : DomainGuard.RequiredText(dataResidency, nameof(dataResidency), 50);
    }

    private void EnsureNotArchived()
    {
        if (Status == OrganizationStatus.Archived)
        {
            throw new DomainException("organization.archived", DomainMessages.OrganizationArchived);
        }
    }
}
