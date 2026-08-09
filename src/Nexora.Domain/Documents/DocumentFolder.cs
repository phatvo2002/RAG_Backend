using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Documents;

public class DocumentFolder : SoftDeletableEntityBase
{
    protected DocumentFolder()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid? ParentFolderId { get; private set; }

    public string Name { get; set; } = string.Empty;

    public Guid OwnerUserId { get; private set; }

}
