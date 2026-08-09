using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Chat;

public class AiTurn : EntityBase
{
    protected AiTurn()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid ConversationId { get; private set; }

    public Guid RequestMessageId { get; private set; }

    public Guid? ResponseMessageId { get; private set; }

    public string Status { get; set; } = string.Empty;

    public string? ModelProvider { get; set; }

    public string? ModelName { get; set; }

    public string? PromptVersion { get; set; }

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    public int? LatencyMs { get; set; }

    public string? NoSourceReason { get; set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ErrorCode { get; set; }
}
