using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Command;

public sealed record StartChatTurnCommand(Guid? ConversationId, string Question, string ModelId, Guid ClientRequestId) : IRequest<ChatTurnStart>;

public sealed class StartChatTurnCommandHandler(IChatService chatService)
    : IRequestHandler<StartChatTurnCommand, ChatTurnStart>
{
    public Task<ChatTurnStart> Handle(StartChatTurnCommand request, CancellationToken cancellationToken) =>
        chatService.StartTurnAsync(request.ConversationId, request.Question, request.ModelId, request.ClientRequestId, cancellationToken);
}
