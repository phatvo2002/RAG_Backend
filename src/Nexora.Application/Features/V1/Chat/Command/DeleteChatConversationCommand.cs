using MediatR;
using Nexora.Application.Common.Abstractions.Chat;

namespace Nexora.Application.Features.V1.Chat.Command;

public sealed record DeleteChatConversationCommand(Guid ConversationId) : IRequest<bool>;

public sealed class DeleteChatConversationCommandHandler(IChatWorkflow chatService)
    : IRequestHandler<DeleteChatConversationCommand, bool>
{
    public Task<bool> Handle(DeleteChatConversationCommand request, CancellationToken cancellationToken) =>
        chatService.DeleteConversationAsync(request.ConversationId, cancellationToken);
}
