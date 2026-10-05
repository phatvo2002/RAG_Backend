using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Infrastructure.Rag;

public sealed class ChatProviderService(
    ChatCipher cipher,
    ProviderKeyCipher providerCipher,
    AccountSettingsService settings,
    ChatModelStreamer models) : IChatProviderService
{
    public async Task<ChatRuntimeConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        var keys = await settings.ListKeysAsync(cancellationToken);
        var choices = cipher.IsConfigured && providerCipher.IsConfigured
            ? models.AvailableModels(keys.Keys.Any(x => x.Provider == "gemini"),
                keys.Keys.Any(x => x.Provider == "claude" && keys.ClaudeEndpoints.Any(e => e.Id == x.EndpointId)))
            : [];
        var error = !cipher.IsConfigured ? "Chưa cấu hình khóa mã hóa lịch sử chat trên máy chủ." :
            !providerCipher.IsConfigured ? "Chưa cấu hình khóa mã hóa API của người dùng trên máy chủ." :
            choices.Count == 0 ? "Hãy thêm khóa Gemini hoặc Claude trong Thông tin cá nhân để bắt đầu chat." : null;

        return new ChatRuntimeConfiguration(cipher.IsConfigured, providerCipher.IsConfigured, choices, error);
    }

    public Task<ProviderCredential?> GetCredentialAsync(string provider, CancellationToken cancellationToken) =>
        settings.GetCredentialAsync(provider, cancellationToken);

    public IAsyncEnumerable<string> StreamAsync(ChatModelChoice model, IReadOnlyList<ChatPromptMessage> history,
        string augmentedContext, ProviderCredential credential, CancellationToken cancellationToken) =>
        models.StreamAsync(model, history, augmentedContext, credential, cancellationToken);
}
