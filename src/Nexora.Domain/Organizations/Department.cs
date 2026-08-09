using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.Exceptions;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations.Enums;

namespace Nexora.Domain.Organizations;

public sealed class Department : SoftDeletableEntityBase
{
    private Department()
    {
    }

    private Department(Guid id, Guid organizationId, DepartmentCode code, string name, Guid? parentDepartmentId, Guid? managerUserId, DateTimeOffset now)
        : base(id, now)
    {
        OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId));
        Code = code;
        Name = DomainGuard.RequiredText(name, nameof(name), 200);
        ParentDepartmentId = parentDepartmentId;
        ManagerUserId = managerUserId;
        Status = DepartmentStatus.Active;
    }

    public Guid OrganizationId { get; private set; }

    public Guid? ParentDepartmentId { get; private set; }

    public DepartmentCode Code { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public Guid? ManagerUserId { get; private set; }

    public DepartmentStatus Status { get; private set; }

    public Organization? Organization { get; private set; }

    public Department? ParentDepartment { get; private set; }

    private readonly List<Department> _children = new();

    public IReadOnlyCollection<Department> Children => _children;

    private readonly List<DepartmentMembership> _memberships = new();

    public IReadOnlyCollection<DepartmentMembership> Memberships => _memberships;

    public static Department Create(Guid organizationId, string code, string name, DateTimeOffset now, Guid? parentDepartmentId = null, Guid? managerUserId = null)
    {
        return new Department(Guid.NewGuid(), organizationId, DepartmentCode.Create(code), name, parentDepartmentId, managerUserId, now);
    }

    public void UpdateName(string name)
    {
        EnsureActive();
        Name = DomainGuard.RequiredText(name, nameof(name), 200);
    }

    public void UpdateParent(Guid? parentDepartmentId)
    {
        EnsureActive();

        if (parentDepartmentId == Id)
        {
            throw new DomainException("department.self_parent", DomainMessages.DepartmentSelfParent);
        }

        ParentDepartmentId = parentDepartmentId;
    }

    public void UpdateManager(Guid managerUserId)
    {
        EnsureActive();
        ManagerUserId = DomainGuard.RequiredId(managerUserId, nameof(managerUserId));
    }

    public void RemoveManager()
    {
        EnsureActive();
        ManagerUserId = null;
    }

    public void Archive()
    {
        if (Status == DepartmentStatus.Archived)
        {
            throw new DomainException("department.already_archived", DomainMessages.DepartmentAlreadyArchived);
        }

        Status = DepartmentStatus.Archived;
    }

    private void EnsureActive()
    {
        if (Status != DepartmentStatus.Active)
        {
            throw new DomainException("department.not_active", DomainMessages.DepartmentNotActive);
        }
    }
}
