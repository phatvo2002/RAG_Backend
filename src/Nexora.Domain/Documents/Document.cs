using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common;

namespace Nexora.Domain.Documents;

public class Document : SoftDeletableEntityBase
{
    protected Document()
    {
    }

    private Document(Guid id, DateTimeOffset now)
        : base(id, now)
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public Guid? DepartmentId { get; private set; }

    public Guid? FolderId { get; private set; }

    public string Title { get; set; } = string.Empty;

    public Guid? CurrentVersionId { get; private set; }

    public string Status { get; set; } = string.Empty;

    public static Document Create(Guid organizationId, Guid departmentId, Guid ownerUserId, string title, DateTimeOffset now)
    {
        var document = new Document(Guid.NewGuid(), now)
        {
            OrganizationId = DomainGuard.RequiredId(organizationId, nameof(organizationId)),
            DepartmentId = DomainGuard.RequiredId(departmentId, nameof(departmentId)),
            OwnerUserId = DomainGuard.RequiredId(ownerUserId, nameof(ownerUserId)),
            Title = DomainGuard.RequiredText(title, nameof(title), 300),
            Status = "UPLOADING"
        };
        return document;
    }

    public void SetCurrentVersion(Guid versionId)
    {
        CurrentVersionId = DomainGuard.RequiredId(versionId, nameof(versionId));
    }

    public void SetFolder(Guid folderId)
    {
        FolderId = DomainGuard.RequiredId(folderId, nameof(folderId));
    }

    public void SetStatus(string status)
    {
        Status = DomainGuard.RequiredText(status, nameof(status), 30);
    }

}
