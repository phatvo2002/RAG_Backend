using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Chat.Command;

public sealed record FailChatTurnCommand(ChatTurnStart Start, string Code) : IRequest;

public sealed class FailChatTurnCommandHandler(IChatWorkflow chatService) : IRequestHandler<FailChatTurnCommand>
{
    public async Task Handle(FailChatTurnCommand request, CancellationToken cancellationToken) =>
        await chatService.FailTurnAsync(request.Start, request.Code);
}
