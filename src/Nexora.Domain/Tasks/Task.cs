using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Tasks;

public class Task : SoftDeletableEntityBase
{
    protected Task()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? DepartmentId { get; private set; }

    public Guid? ParentTaskId { get; private set; }

    public string Title { get; set; } = string.Empty;

    public byte[]? DescriptionCiphertext { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public Guid? AssigneeUserId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

}
