using MediatR;
using Nexora.Application.Features.V1.Account;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record GetChatCredentialQuery(string Provider) : IRequest<ProviderCredential?>;

public sealed class GetChatCredentialQueryHandler(ISender sender)
    : IRequestHandler<GetChatCredentialQuery, ProviderCredential?>
{
    public Task<ProviderCredential?> Handle(GetChatCredentialQuery request, CancellationToken cancellationToken) =>
        sender.Send(new GetProviderCredentialQuery(request.Provider), cancellationToken);
}
