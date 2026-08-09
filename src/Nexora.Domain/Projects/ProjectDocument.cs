using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Projects;

public class ProjectDocument : CompositeSoftDeletableEntityBase
{
    protected ProjectDocument()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid DocumentId { get; private set; }

    public Guid LinkedByUserId { get; private set; }

    public DateTimeOffset LinkedAt { get; set; }

}
