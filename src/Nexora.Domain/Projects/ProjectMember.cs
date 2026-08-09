using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Projects;

public class ProjectMember : EntityBase
{
    protected ProjectMember()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid UserId { get; private set; }

    public string MembershipType { get; set; } = string.Empty;

    public DateTimeOffset JoinedAt { get; set; }

    public DateTimeOffset? LeftAt { get; set; }

    public Guid? LeftByUserId { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }
}
