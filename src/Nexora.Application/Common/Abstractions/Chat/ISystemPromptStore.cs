using Nexora.Domain.Chat;

namespace Nexora.Application.Common.Abstractions.Chat;

public static class SystemPromptTypes
{
    public const string Chat = "CHAT";
    public const string MeetingTranslation = "MEETING_TRANSLATION";
    public static readonly IReadOnlyList<string> All = [Chat, MeetingTranslation];
}

public static class SystemPromptDefaults
{
    public const string Chat = "You are Nexora AI, a helpful conversational assistant. Reply in the user's language with a natural, warm, direct tone. For greetings, thanks, small talk, questions about your capabilities, and general knowledge, answer like an ordinary assistant using general knowledge and conversation context. Keep greetings brief. Use retrieved passages as optional context only for company-specific questions or questions about a specific document. If relevant passages do not support a company-specific answer, say you do not have enough information. Do not imply general knowledge came from company documents. Answer concisely, usually in one to three sentences, and use more detail when asked.";
    public const string MeetingTranslation = "Translate meeting speech faithfully. Return only the translation. Preserve names, numbers and meaning.";
}

public sealed record SystemPromptVersionData(Guid Id, int VersionNo, string Instructions, string Status,
    Guid? CreatedByUserId, DateTimeOffset CreatedAt, Guid? PublishedByUserId, DateTimeOffset? PublishedAt);

public sealed record SystemPromptData(Guid Id, string PromptType, long RowVersion,
    SystemPromptVersionData ActiveVersion, SystemPromptVersionData? Draft);

public interface ISystemPromptStore
{
    Task<IReadOnlyList<SystemPromptData>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<SystemPromptData?> GetAsync(Guid organizationId, string promptType, CancellationToken cancellationToken);
    Task<IReadOnlyList<SystemPromptVersionData>> GetVersionsAsync(Guid organizationId, string promptType, CancellationToken cancellationToken);
    Task<SystemPromptVersionData> GetActiveVersionAsync(Guid organizationId, string promptType, CancellationToken cancellationToken);
    Task<SystemPromptVersionData> SaveDraftAsync(Guid organizationId, string promptType, string instructions, Guid userId, CancellationToken cancellationToken);
    Task<SystemPromptVersionData?> PublishDraftAsync(Guid organizationId, string promptType, Guid userId, CancellationToken cancellationToken);
    Task<SystemPromptVersionData?> RestoreAsync(Guid organizationId, string promptType, Guid versionId, Guid userId, CancellationToken cancellationToken);
}
