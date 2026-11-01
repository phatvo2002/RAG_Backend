using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Chat;

public sealed class OrganizationSystemPrompt : AuditableEntityBase
{
    private OrganizationSystemPrompt() { }

    private OrganizationSystemPrompt(Guid id, Guid organizationId, string promptType, DateTimeOffset now)
        : base(id, now)
    {
        OrganizationId = organizationId;
        PromptType = promptType;
    }

    public Guid OrganizationId { get; private set; }
    public string PromptType { get; private set; } = string.Empty;
    public Guid? ActiveVersionId { get; private set; }
    public SystemPromptVersion? ActiveVersion { get; private set; }

    public static OrganizationSystemPrompt Create(Guid organizationId, string promptType, DateTimeOffset now) =>
        new(Guid.NewGuid(), organizationId, promptType, now);

    public void Activate(SystemPromptVersion version)
    {
        if (version.SystemPromptId != Id || version.Status != SystemPromptVersion.PublishedStatus)
            throw new InvalidOperationException("The active prompt version must be a published version of this prompt.");
        ActiveVersionId = version.Id;
    }
}
