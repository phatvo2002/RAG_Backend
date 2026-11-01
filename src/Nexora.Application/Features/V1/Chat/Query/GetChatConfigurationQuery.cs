using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record GetChatConfigurationQuery : IRequest<ChatRuntimeConfiguration>;

public sealed class GetChatConfigurationQueryHandler(IChatProviderService providerService,
    ISystemPromptStore? prompts = null, Nexora.Application.Common.Abstractions.IDefaultOrganizationProvider? organizations = null)
    : IRequestHandler<GetChatConfigurationQuery, ChatRuntimeConfiguration>
{
    public async Task<ChatRuntimeConfiguration> Handle(GetChatConfigurationQuery request, CancellationToken cancellationToken)
    {
        var choices = providerService.IsChatCipherConfigured ? providerService.AvailableModels() : [];
        var error = !providerService.IsChatCipherConfigured ? "Chưa cấu hình khóa mã hóa lịch sử chat trên máy chủ." :
            choices.Count == 0 ? "Chưa cấu hình mô hình Ollama trên máy chủ." : null;
        var estimatedSystemPromptTokens = providerService.EstimatedSystemPromptTokens;
        if (prompts is not null && organizations is not null)
        {
            var organizationId = await organizations.GetOrganizationIdAsync(cancellationToken);
            if (organizationId.HasValue)
            {
                var active = await prompts.GetActiveVersionAsync(organizationId.Value, SystemPromptTypes.Chat, cancellationToken);
                estimatedSystemPromptTokens = providerService.EstimatePromptTokens([], string.Empty, active.Instructions);
            }
        }
        return new ChatRuntimeConfiguration(providerService.IsChatCipherConfigured, choices, error,
            providerService.ContextWindowTokens, providerService.MaxOutputTokens,
            providerService.EstimatedRetrievedContextTokens, estimatedSystemPromptTokens, 2000);
    }
}
