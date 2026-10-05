using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record GetChatCredentialQuery(string Provider) : IRequest<ProviderCredential?>;

public sealed class GetChatCredentialQueryHandler(IChatProviderService providerService)
    : IRequestHandler<GetChatCredentialQuery, ProviderCredential?>
{
    public Task<ProviderCredential?> Handle(GetChatCredentialQuery request, CancellationToken cancellationToken) =>
        providerService.GetCredentialAsync(request.Provider, cancellationToken);
}
