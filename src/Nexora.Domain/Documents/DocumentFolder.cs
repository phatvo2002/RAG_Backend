using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common;

namespace Nexora.Domain.Documents;

public class DocumentFolder : SoftDeletableEntityBase
{
    protected DocumentFolder()
    {
    }

    private DocumentFolder(Guid id, DateTimeOffset now, Guid organizationId, Guid departmentId, Guid ownerUserId, string name, Guid? parentFolderId)
        : base(id, now)
    {
        OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId));
        DepartmentId = DomainGuard.RequiredId(departmentId, nameof(departmentId));
        OwnerUserId = DomainGuard.RequiredId(ownerUserId, nameof(ownerUserId));
        Name = DomainGuard.RequiredText(name, nameof(name), 200);
        ParentFolderId = parentFolderId;
    }

    public Guid OrganizationId { get; private set; }

    public Guid? DepartmentId { get; private set; }

    public Guid? ParentFolderId { get; private set; }

    public string Name { get; set; } = string.Empty;

    public Guid OwnerUserId { get; private set; }

    public static DocumentFolder Create(Guid organizationId, Guid departmentId, Guid ownerUserId, string name, DateTimeOffset now, Guid? parentFolderId = null)
    {
        return new DocumentFolder(Guid.NewGuid(), now, organizationId, departmentId, ownerUserId, name, parentFolderId);
    }

}
