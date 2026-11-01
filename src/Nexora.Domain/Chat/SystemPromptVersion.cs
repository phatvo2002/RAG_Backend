using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Chat;

public sealed class SystemPromptVersion : EntityBase
{
    public const string DraftStatus = "DRAFT";
    public const string PublishedStatus = "PUBLISHED";

    private SystemPromptVersion() { }

    private SystemPromptVersion(Guid id, Guid systemPromptId, int versionNo, string instructions,
        Guid? createdByUserId, DateTimeOffset now, bool published)
        : base(id)
    {
        SystemPromptId = systemPromptId;
        VersionNo = versionNo;
        Instructions = instructions;
        Status = published ? PublishedStatus : DraftStatus;
        CreatedByUserId = createdByUserId;
        CreatedAt = now;
        if (published)
        {
            PublishedByUserId = createdByUserId;
            PublishedAt = now;
        }
    }

    public Guid SystemPromptId { get; private set; }
    public int VersionNo { get; private set; }
    public string Instructions { get; private set; } = string.Empty;
    public string Status { get; private set; } = DraftStatus;
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? PublishedByUserId { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public OrganizationSystemPrompt SystemPrompt { get; private set; } = null!;

    public static SystemPromptVersion Create(Guid promptId, int versionNo, string instructions,
        Guid? userId, DateTimeOffset now, bool published = false) =>
        new(Guid.NewGuid(), promptId, versionNo, instructions, userId, now, published);

    public void UpdateDraft(string instructions)
    {
        if (Status != DraftStatus) throw new InvalidOperationException("Published prompt versions are immutable.");
        Instructions = instructions;
    }

    public void Publish(Guid? userId, DateTimeOffset now)
    {
        if (Status != DraftStatus) throw new InvalidOperationException("Only a draft prompt can be published.");
        Status = PublishedStatus;
        PublishedByUserId = userId;
        PublishedAt = now;
    }
}
