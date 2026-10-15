using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Account;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record GetChatConfigurationQuery : IRequest<ChatRuntimeConfiguration>;

public sealed class GetChatConfigurationQueryHandler(IChatProviderService providerService, ISender sender)
    : IRequestHandler<GetChatConfigurationQuery, ChatRuntimeConfiguration>
{
    public async Task<ChatRuntimeConfiguration> Handle(GetChatConfigurationQuery request, CancellationToken cancellationToken)
    {
        var keys = await sender.Send(new GetProviderKeysQuery(), cancellationToken);
        var choices = providerService.IsChatCipherConfigured && providerService.IsProviderCipherConfigured
            ? providerService.AvailableModels(keys.Keys.Any(x => x.Provider == "gemini"),
                keys.Keys.Any(x => x.Provider == "claude" && keys.ClaudeEndpoints.Any(e => e.Id == x.EndpointId)))
            : [];
        var error = !providerService.IsChatCipherConfigured ? "Chưa cấu hình khóa mã hóa lịch sử chat trên máy chủ." :
            !providerService.IsProviderCipherConfigured ? "Chưa cấu hình khóa mã hóa API của người dùng trên máy chủ." :
            choices.Count == 0 ? "Hãy thêm khóa Gemini hoặc Claude trong Thông tin cá nhân để bắt đầu chat." : null;
        return new ChatRuntimeConfiguration(providerService.IsChatCipherConfigured, providerService.IsProviderCipherConfigured, choices, error);
    }
}
