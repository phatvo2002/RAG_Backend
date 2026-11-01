namespace Nexora.Application.Features.V1.SystemPrompts.Model;

public sealed record SystemPromptVersionResponse(Guid Id, int VersionNo, string Instructions, string Status,
    Guid? CreatedByUserId, DateTimeOffset CreatedAt, Guid? PublishedByUserId, DateTimeOffset? PublishedAt);

public sealed record SystemPromptResponse(Guid Id, string PromptType, long RowVersion,
    SystemPromptVersionResponse ActiveVersion, SystemPromptVersionResponse? Draft);

public sealed record SaveSystemPromptDraftRequest(string Instructions);
public sealed record PreviewSystemPromptRequest(string Question, string Instructions);
public sealed record SystemPromptPreviewResponse(string PromptType, string Answer);
public sealed record MeetingAgentSystemPromptResponse(Guid VersionId, string Instructions);
