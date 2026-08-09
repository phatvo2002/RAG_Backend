using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Chat;

public class ConversationContext : CreatedEntityBase
{
    protected ConversationContext()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid ConversationId { get; private set; }

    public string ScopeType { get; set; } = string.Empty;

    public Guid? DepartmentId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? DocumentId { get; private set; }

    public Guid SelectedByUserId { get; private set; }

}
