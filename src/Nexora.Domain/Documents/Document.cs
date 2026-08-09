using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Documents;

public class Document : SoftDeletableEntityBase
{
    protected Document()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public Guid? FolderId { get; private set; }

    public string Title { get; set; } = string.Empty;

    public Guid? CurrentVersionId { get; private set; }

    public string Status { get; set; } = string.Empty;

}
