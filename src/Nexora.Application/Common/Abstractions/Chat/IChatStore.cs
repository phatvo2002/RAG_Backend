using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;
using Nexora.Domain.Chat;

namespace Nexora.Application.Common.Abstractions.Chat;

public interface IChatWorkflow
{
    Task<IReadOnlyList<ChatConversationDto>> ListConversationsAsync(int skip, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid conversationId, int? beforeSequenceNo, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageDto>> VisibleHistoryMessagesAsync(Guid conversationId, Guid requestMessageId, CancellationToken cancellationToken);
    Task<ChatTurnStart> StartTurnAsync(Guid? conversationId, string question, string modelId, Guid clientRequestId, CancellationToken cancellationToken);
    Task<ChatMessageDto> CompleteTurnAsync(ChatTurnStart start, string answer,
        IReadOnlyList<RetrievedChunkResponse> sources, bool noSource, CancellationToken cancellationToken);
    Task FailTurnAsync(ChatTurnStart start, string code);
    Task<bool> DeleteMessageAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken);
}

public interface IChatStore
{
    Task<IReadOnlyList<Conversation>> ListConversationsAsync(Guid organizationId, Guid userId, int skip, CancellationToken cancellationToken);
    Task<Conversation?> GetConversationAsync(Guid organizationId, Guid userId, Guid conversationId, CancellationToken cancellationToken);
    Task<bool> RequestExistsAsync(Guid organizationId, Guid clientRequestId, CancellationToken cancellationToken);
    Task<int> NextSequenceAsync(Guid organizationId, Guid conversationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid organizationId, Guid conversationId, int? beforeSequenceNo, Guid? excludeMessageId, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiTurn>> GetStaleTurnsAsync(Guid organizationId, Guid conversationId, DateTimeOffset cutoff, CancellationToken cancellationToken);
    Task<IReadOnlyList<AiTurn>> GetTurnsForMessagesAsync(Guid organizationId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<Citation>> GetCitationsAsync(Guid organizationId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken);
    Task<AiTurn?> GetTurnAsync(Guid turnId, Guid? organizationId, Guid? conversationId, CancellationToken cancellationToken);
    Task<Message?> GetMessageAsync(Guid organizationId, Guid conversationId, Guid messageId, CancellationToken cancellationToken);
    void AddConversation(Conversation conversation);
    void AddMessage(Message message);
    void AddTurn(AiTurn turn);
    void AddCitation(Citation citation);
    void SetConversationUpdatedAt(Conversation conversation, DateTimeOffset now);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IChatProviderService
{
    bool IsChatCipherConfigured { get; }
    bool IsProviderCipherConfigured { get; }
    IReadOnlyList<ChatModelChoice> AvailableModels(bool hasGeminiKey, bool hasClaudeKey);
    IAsyncEnumerable<string> StreamAsync(ChatModelChoice model, IReadOnlyList<ChatPromptMessage> history,
        string augmentedContext, ProviderCredential credential, CancellationToken cancellationToken);
}
