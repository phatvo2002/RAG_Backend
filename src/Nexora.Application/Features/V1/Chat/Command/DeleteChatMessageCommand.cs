using MediatR;
using Nexora.Application.Common.Abstractions.Chat;

namespace Nexora.Application.Features.V1.Chat.Command;

public sealed record DeleteChatMessageCommand(Guid ConversationId, Guid MessageId) : IRequest<bool>;

public sealed class DeleteChatMessageCommandHandler(IChatWorkflow chatService)
    : IRequestHandler<DeleteChatMessageCommand, bool>
{
    public Task<bool> Handle(DeleteChatMessageCommand request, CancellationToken cancellationToken) =>
        chatService.DeleteMessageAsync(request.ConversationId, request.MessageId, cancellationToken);
}
