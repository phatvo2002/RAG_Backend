using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record ListChatConversationsQuery(int Skip) : IRequest<IReadOnlyList<ChatConversationDto>>;

public sealed class ListChatConversationsQueryHandler(IChatService chatService)
    : IRequestHandler<ListChatConversationsQuery, IReadOnlyList<ChatConversationDto>>
{
    public Task<IReadOnlyList<ChatConversationDto>> Handle(ListChatConversationsQuery request, CancellationToken cancellationToken) =>
        chatService.ListConversationsAsync(request.Skip, cancellationToken);
}
