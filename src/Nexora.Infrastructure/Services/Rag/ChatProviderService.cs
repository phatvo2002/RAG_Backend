using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Infrastructure.Services.Rag;

public sealed class ChatProviderService(
    ChatCipher cipher,
    ChatModelStreamer models,
    Microsoft.Extensions.Options.IOptions<ChatOptions> options) : IChatProviderService
{
    public bool IsChatCipherConfigured => cipher.IsConfigured;
    public int ContextWindowTokens => options.Value.ContextWindowTokens;
    public int MaxOutputTokens => options.Value.MaxOutputTokens;
    public int EstimatedRetrievedContextTokens => options.Value.EstimatedRetrievedContextTokens;
    public int EstimatedSystemPromptTokens => models.EstimatePromptTokens([], string.Empty, SystemPromptDefaults.Chat);
    public int EstimatePromptTokens(IReadOnlyList<ChatPromptMessage> history, string augmentedContext, string customInstructions = "") =>
        models.EstimatePromptTokens(history, augmentedContext, customInstructions);
    public IReadOnlyList<ChatModelChoice> AvailableModels() => models.AvailableModels();

    public IAsyncEnumerable<string> StreamAsync(ChatModelChoice model, IReadOnlyList<ChatPromptMessage> history,
        string augmentedContext, string customInstructions, CancellationToken cancellationToken) =>
        models.StreamAsync(model, history, augmentedContext, customInstructions, cancellationToken);

    public Task<string> PreviewTranslationAsync(string text, string customInstructions, CancellationToken cancellationToken) =>
        models.PreviewTranslationAsync(text, customInstructions, cancellationToken);
}
