using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Command;

public sealed record StartChatTurnCommand(Guid? ConversationId, string Question, string ModelId, Guid ClientRequestId) : IRequest<ChatTurnStart>;

public sealed class StartChatTurnCommandHandler(IChatWorkflow chatService)
    : IRequestHandler<StartChatTurnCommand, ChatTurnStart>
{
    public Task<ChatTurnStart> Handle(StartChatTurnCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 2000 || request.ClientRequestId == Guid.Empty)
            throw new ArgumentException("A question of at most 2000 characters and a request ID are required.");
        return chatService.StartTurnAsync(request.ConversationId, request.Question, request.ModelId, request.ClientRequestId, cancellationToken);
    }
}
