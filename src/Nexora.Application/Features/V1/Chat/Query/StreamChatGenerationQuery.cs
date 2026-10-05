using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record StreamChatGenerationQuery(ChatModelChoice Model, IReadOnlyList<ChatPromptMessage> History,
    string AugmentedContext, ProviderCredential Credential) : IRequest<IAsyncEnumerable<string>>;

public sealed class StreamChatGenerationQueryHandler(IChatProviderService providerService)
    : IRequestHandler<StreamChatGenerationQuery, IAsyncEnumerable<string>>
{
    public Task<IAsyncEnumerable<string>> Handle(StreamChatGenerationQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(providerService.StreamAsync(request.Model, request.History, request.AugmentedContext,
            request.Credential, cancellationToken));
}
