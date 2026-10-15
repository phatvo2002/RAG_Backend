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

    public static AiTurn Create(Guid id, Guid organizationId, Guid conversationId, Guid requestMessageId,
        string correlationId, string provider, string model, string promptVersion, DateTimeOffset now) => new(id)
    {
        OrganizationId = organizationId,
        ConversationId = conversationId,
        RequestMessageId = requestMessageId,
        CorrelationId = correlationId,
        ModelProvider = provider,
        ModelName = model,
        PromptVersion = promptVersion,
        Status = "PENDING",
        StartedAt = now
    };

    public void Complete(Guid responseMessageId, DateTimeOffset now)
    {
        ResponseMessageId = responseMessageId;
        Status = "COMPLETED";
        CompletedAt = now;
    }

    private AiTurn(Guid id) : base(id) { }
}
