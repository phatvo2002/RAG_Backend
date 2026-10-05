using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record ListChatMessagesQuery(Guid ConversationId, int? BeforeSequenceNo) : IRequest<IReadOnlyList<ChatMessageDto>>;

public sealed class ListChatMessagesQueryHandler(IChatService chatService)
    : IRequestHandler<ListChatMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    public Task<IReadOnlyList<ChatMessageDto>> Handle(ListChatMessagesQuery request, CancellationToken cancellationToken) =>
        chatService.ListMessagesAsync(request.ConversationId, request.BeforeSequenceNo, cancellationToken);
}
