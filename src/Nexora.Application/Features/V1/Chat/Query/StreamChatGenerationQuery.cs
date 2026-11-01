using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record StreamChatGenerationQuery(ChatModelChoice Model, IReadOnlyList<ChatPromptMessage> History,
    string AugmentedContext, string CustomInstructions) : IRequest<IAsyncEnumerable<string>>;

public sealed class StreamChatGenerationQueryHandler(IChatProviderService providerService)
    : IRequestHandler<StreamChatGenerationQuery, IAsyncEnumerable<string>>
{
    public Task<IAsyncEnumerable<string>> Handle(StreamChatGenerationQuery request, CancellationToken cancellationToken)
    {
        var estimate = providerService.EstimatePromptTokens(request.History, request.AugmentedContext, request.CustomInstructions);
        var available = providerService.ContextWindowTokens - providerService.MaxOutputTokens;
        if (estimate > available) throw new ChatContextWindowExceededException(estimate, available);
        return Task.FromResult(providerService.StreamAsync(request.Model, request.History, request.AugmentedContext,
            request.CustomInstructions, cancellationToken));
    }
}
