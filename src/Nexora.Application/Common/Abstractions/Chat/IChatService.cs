using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;

namespace Nexora.Application.Common.Abstractions.Chat;

public interface IChatService
{
    Task<IReadOnlyList<ChatConversationDto>> ListConversationsAsync(int skip, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid conversationId, int? beforeSequenceNo, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatPromptMessage>> PromptHistoryAsync(Guid conversationId, Guid requestMessageId, CancellationToken cancellationToken);
    Task<ChatTurnStart> StartTurnAsync(Guid? conversationId, string question, string modelId, Guid clientRequestId, CancellationToken cancellationToken);
    Task<ChatMessageDto> CompleteTurnAsync(ChatTurnStart start, string answer,
        IReadOnlyList<RetrievedChunkResponse> sources, bool noSource, CancellationToken cancellationToken);
    Task FailTurnAsync(ChatTurnStart start, string code);
    Task<bool> DeleteMessageAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken);
}

public interface IChatProviderService
{
    Task<ChatRuntimeConfiguration> GetConfigurationAsync(CancellationToken cancellationToken);
    Task<ProviderCredential?> GetCredentialAsync(string provider, CancellationToken cancellationToken);
    IAsyncEnumerable<string> StreamAsync(ChatModelChoice model, IReadOnlyList<ChatPromptMessage> history,
        string augmentedContext, ProviderCredential credential, CancellationToken cancellationToken);
}
