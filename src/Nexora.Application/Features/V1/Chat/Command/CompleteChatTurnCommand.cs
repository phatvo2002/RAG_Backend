using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;

namespace Nexora.Application.Features.V1.Chat.Command;

public sealed record CompleteChatTurnCommand(ChatTurnStart Start, string Answer,
    IReadOnlyList<RetrievedChunkResponse> Sources, bool NoSource) : IRequest<ChatMessageDto>;

public sealed class CompleteChatTurnCommandHandler(IChatWorkflow chatService, IDocumentRetrievalStore retrievalStore,
    IDefaultOrganizationProvider organizationProvider, IUserContext userContext, IClock clock)
    : IRequestHandler<CompleteChatTurnCommand, ChatMessageDto>
{
    public async Task<ChatMessageDto> Handle(CompleteChatTurnCommand request, CancellationToken cancellationToken)
    {
        if (request.Sources.Count > 0)
        {
            var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken);
            var userId = userContext.UserId;
            if (!organizationId.HasValue || !userId.HasValue)
                throw new UnauthorizedAccessException("An organization and signed-in user are required.");
            var accessible = await retrievalStore.GetAccessibleChunkSourcesAsync(organizationId.Value, userId.Value, clock.UtcNow,
                request.Sources.Select(x => x.ChunkId).ToArray(), cancellationToken);
            var allowed = accessible.ToDictionary(x => x.ChunkId);
            if (request.Sources.Any(x => !allowed.TryGetValue(x.ChunkId, out var current) || current.DocumentId != x.DocumentId))
                throw new UnauthorizedAccessException("A supporting document is no longer accessible.");
        }
        return await chatService.CompleteTurnAsync(request.Start, request.Answer, request.Sources, request.NoSource, cancellationToken);
    }
}
