using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record GetChatPromptHistoryQuery(Guid ConversationId, Guid RequestMessageId) : IRequest<IReadOnlyList<ChatPromptMessage>>;

public sealed class GetChatPromptHistoryQueryHandler(IChatService chatService)
    : IRequestHandler<GetChatPromptHistoryQuery, IReadOnlyList<ChatPromptMessage>>
{
    public Task<IReadOnlyList<ChatPromptMessage>> Handle(GetChatPromptHistoryQuery request, CancellationToken cancellationToken) =>
        chatService.PromptHistoryAsync(request.ConversationId, request.RequestMessageId, cancellationToken);
}
