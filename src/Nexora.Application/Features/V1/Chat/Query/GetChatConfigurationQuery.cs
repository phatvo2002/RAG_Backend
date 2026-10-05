using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record GetChatConfigurationQuery : IRequest<ChatRuntimeConfiguration>;

public sealed class GetChatConfigurationQueryHandler(IChatProviderService providerService)
    : IRequestHandler<GetChatConfigurationQuery, ChatRuntimeConfiguration>
{
    public Task<ChatRuntimeConfiguration> Handle(GetChatConfigurationQuery request, CancellationToken cancellationToken) =>
        providerService.GetConfigurationAsync(cancellationToken);
}
