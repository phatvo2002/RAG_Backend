using MediatR;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;

namespace Nexora.Application.Features.V1.Chat.Command;

public sealed record CompleteChatTurnCommand(ChatTurnStart Start, string Answer,
    IReadOnlyList<RetrievedChunkResponse> Sources, bool NoSource) : IRequest<ChatMessageDto>;

public sealed class CompleteChatTurnCommandHandler(IChatService chatService)
    : IRequestHandler<CompleteChatTurnCommand, ChatMessageDto>
{
    public Task<ChatMessageDto> Handle(CompleteChatTurnCommand request, CancellationToken cancellationToken) =>
        chatService.CompleteTurnAsync(request.Start, request.Answer, request.Sources, request.NoSource, cancellationToken);
}
