using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Query;

public sealed record GetChatPromptHistoryQuery(Guid ConversationId, Guid RequestMessageId) : IRequest<IReadOnlyList<ChatPromptMessage>>;

public sealed class GetChatPromptHistoryQueryHandler(IChatWorkflow chatService)
    : IRequestHandler<GetChatPromptHistoryQuery, IReadOnlyList<ChatPromptMessage>>
{
    public async Task<IReadOnlyList<ChatPromptMessage>> Handle(GetChatPromptHistoryQuery request, CancellationToken cancellationToken)
    {
        var visible = await chatService.VisibleHistoryMessagesAsync(request.ConversationId, request.RequestMessageId, cancellationToken);
        var selected = new List<ChatPromptMessage>();
        var characters = 0;
        foreach (var message in visible.Reverse().Where(x => x.Status is not ("FAILED" or "PENDING")))
        {
            if (selected.Count == 10 || characters + message.Content.Length > 12000) break;
            selected.Add(new ChatPromptMessage(message.Role, message.Content));
            characters += message.Content.Length;
        }
        selected.Reverse();
        return selected;
    }
}
