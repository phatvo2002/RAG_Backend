namespace Nexora.Application.Features.V1.Chat.Model;

public sealed record ChatConversationDto(Guid Id, string Title, DateTimeOffset UpdatedAt);
public sealed record ChatSourceDto(Guid DocumentId, Guid ChunkId, string FileName, float Score, int? PageNo, int? CharStart, int? CharEnd);
public sealed record ChatMessageDto(Guid Id, Guid ConversationId, string Role, string Content, int SequenceNo,
    DateTimeOffset CreatedAt, string? Status, string? ModelProvider, string? ModelName, IReadOnlyList<ChatSourceDto> Sources);
public sealed record ChatTurnStart(Guid ConversationId, Guid RequestMessageId, Guid TurnId, ChatModelChoice Model,
    Guid SystemPromptVersionId, string SystemPromptInstructions);
public sealed record ChatModelChoice(string Id, string Name, string Provider, string Model);
public sealed record ChatPromptMessage(string Role, string Content);
public sealed record ProviderCredential(string ApiKey, string EndpointId, string BaseUrl);
public sealed record ChatRuntimeConfiguration(
    bool ChatEncryptionConfigured,
    IReadOnlyList<ChatModelChoice> Models,
    string? ConfigurationError,
    int ContextWindowTokens,
    int MaxOutputTokens,
    int EstimatedRetrievedContextTokens,
    int EstimatedSystemPromptTokens,
    int MaxQuestionCharacters);

public sealed class ChatContextWindowExceededException(int estimatedTokens, int maximumPromptTokens)
    : InvalidOperationException($"The estimated prompt uses {estimatedTokens} tokens, above the available prompt budget of {maximumPromptTokens} tokens.")
{
    public int EstimatedTokens { get; } = estimatedTokens;
    public int MaximumPromptTokens { get; } = maximumPromptTokens;
}
