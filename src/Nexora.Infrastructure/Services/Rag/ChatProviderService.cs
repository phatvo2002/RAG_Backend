using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Infrastructure.Services.Authentication;

namespace Nexora.Infrastructure.Services.Rag;

public sealed class ChatProviderService(
    ChatCipher cipher,
    ProviderKeyCipher providerCipher,
    ChatModelStreamer models) : IChatProviderService
{
    public bool IsChatCipherConfigured => cipher.IsConfigured;
    public bool IsProviderCipherConfigured => providerCipher.IsConfigured;
    public IReadOnlyList<ChatModelChoice> AvailableModels(bool hasGeminiKey, bool hasClaudeKey) =>
        models.AvailableModels(hasGeminiKey, hasClaudeKey);

    public IAsyncEnumerable<string> StreamAsync(ChatModelChoice model, IReadOnlyList<ChatPromptMessage> history,
        string augmentedContext, ProviderCredential credential, CancellationToken cancellationToken) =>
        models.StreamAsync(model, history, augmentedContext, credential, cancellationToken);
}
