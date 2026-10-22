using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;
using Nexora.Domain.Chat;

namespace Nexora.Application.Features.V1.Chat;

public sealed class ChatWorkflow(
    IChatStore store,
    IDefaultOrganizationProvider organizationProvider,
    IUserContext userContext,
    IDocumentRetrievalStore retrievalStore,
    IClock clock,
    IChatCipher cipher,
    IChatModelResolver models) : IChatWorkflow
{
    private async Task<(Guid OrganizationId, Guid UserId)> IdentityAsync(CancellationToken ct)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(ct);
        if (!organizationId.HasValue || !userContext.UserId.HasValue)
            throw new UnauthorizedAccessException("An organization and signed-in user are required.");
        return (organizationId.Value, userContext.UserId.Value);
    }

    private async Task<Conversation> RequireConversationAsync(Guid organizationId, Guid userId, Guid conversationId, CancellationToken ct) =>
        await store.GetConversationAsync(organizationId, userId, conversationId, ct)
        ?? throw new KeyNotFoundException("Conversation was not found.");

    public async Task<IReadOnlyList<ChatConversationDto>> ListConversationsAsync(int skip, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        var rows = await store.ListConversationsAsync(organizationId, userId, skip, ct);
        return rows.Select(x => new ChatConversationDto(x.Id, x.TitleCiphertext is null ? "Cuộc trò chuyện" : cipher.Decrypt(x.TitleCiphertext), x.UpdatedAt)).ToArray();
    }

    public async Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid conversationId, int? beforeSequenceNo, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        await RequireConversationAsync(organizationId, userId, conversationId, ct);
        var now = clock.UtcNow;
        var staleTurns = await store.GetStaleTurnsAsync(organizationId, conversationId, now.AddMinutes(-5), ct);
        foreach (var turn in staleTurns)
        {
            turn.Status = "FAILED";
            turn.ErrorCode = "INTERRUPTED";
            turn.CompletedAt = now;
        }
        if (staleTurns.Count > 0) await store.SaveChangesAsync(ct);
        var rows = await store.GetMessagesAsync(organizationId, conversationId, beforeSequenceNo, null, 50, ct);
        return await VisibleMessagesAsync(organizationId, userId, rows.OrderBy(x => x.SequenceNo).ToArray(), ct);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> VisibleHistoryMessagesAsync(Guid conversationId, Guid requestMessageId, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        await RequireConversationAsync(organizationId, userId, conversationId, ct);
        var rows = await store.GetMessagesAsync(organizationId, conversationId, null, requestMessageId, 100, ct);
        return await VisibleMessagesAsync(organizationId, userId, rows.OrderBy(x => x.SequenceNo).ToArray(), ct);
    }

    public async Task<ChatTurnStart> StartTurnAsync(Guid? conversationId, string question, string modelId, Guid clientRequestId, CancellationToken ct)
    {
        var model = models.Resolve(modelId);
        var (organizationId, userId) = await IdentityAsync(ct);
        if (await store.RequestExistsAsync(organizationId, clientRequestId, ct))
            throw new InvalidOperationException("This chat request was already submitted.");

        Conversation conversation;
        if (conversationId.HasValue)
            conversation = await RequireConversationAsync(organizationId, userId, conversationId.Value, ct);
        else
        {
            conversation = Conversation.Create(Guid.NewGuid(), organizationId, userId,
                cipher.Encrypt(question.Trim()[..Math.Min(question.Trim().Length, 80)]), clock.UtcNow);
            store.AddConversation(conversation);
        }

        var nextSequence = await store.NextSequenceAsync(organizationId, conversation.Id, ct);
        var message = Message.Create(Guid.NewGuid(), organizationId, conversation.Id, "user", userId,
            cipher.Encrypt(question.Trim()), cipher.Hash(question.Trim()), nextSequence + 1, clock.UtcNow);
        var turn = AiTurn.Create(Guid.NewGuid(), organizationId, conversation.Id, message.Id,
            clientRequestId.ToString(), model.Provider, model.Model, RagContextComposer.PromptVersion, clock.UtcNow);
        store.AddMessage(message);
        store.AddTurn(turn);
        await store.SaveChangesAsync(ct);
        return new ChatTurnStart(conversation.Id, message.Id, turn.Id, model);
    }

    public async Task<ChatMessageDto> CompleteTurnAsync(ChatTurnStart start, string answer,
        IReadOnlyList<RetrievedChunkResponse> sources, bool noSource, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        var conversation = await RequireConversationAsync(organizationId, userId, start.ConversationId, ct);
        var turn = await store.GetTurnAsync(start.TurnId, organizationId, null, ct)
            ?? throw new KeyNotFoundException("Chat turn was not found.");
        var nextSequence = await store.NextSequenceAsync(organizationId, start.ConversationId, ct);
        var message = Message.Create(Guid.NewGuid(), organizationId, start.ConversationId, "assistant", null,
            cipher.Encrypt(answer), cipher.Hash(answer), nextSequence + 1, clock.UtcNow);
        store.AddMessage(message);
        foreach (var source in sources.Select((value, index) => (value, index)))
            store.AddCitation(Citation.Create(Guid.NewGuid(), organizationId, message.Id, source.value.ChunkId,
                source.index + 1, source.value.PageNo, source.value.CharStart, source.value.CharEnd, source.value.Score, clock.UtcNow));
        turn.ModelProvider = start.Model.Provider;
        turn.ModelName = start.Model.Model;
        if (noSource) turn.NoSourceReason = "NO_AUTHORIZED_SOURCE";
        turn.Complete(message.Id, clock.UtcNow);
        store.SetConversationUpdatedAt(conversation, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return new ChatMessageDto(message.Id, start.ConversationId, "assistant", answer, message.SequenceNo, message.CreatedAt,
            turn.Status, start.Model.Provider, start.Model.Model, sources.Select(x => new ChatSourceDto(x.DocumentId, x.ChunkId, x.FileName, x.Score, x.PageNo, x.CharStart, x.CharEnd)).ToArray());
    }

    public async Task FailTurnAsync(ChatTurnStart start, string code)
    {
        var turn = await store.GetTurnAsync(start.TurnId, null, start.ConversationId, CancellationToken.None);
        if (turn is null || turn.Status == "COMPLETED") return;
        turn.Status = "FAILED";
        turn.ErrorCode = code;
        turn.CompletedAt = clock.UtcNow;
        await store.SaveChangesAsync(CancellationToken.None);
    }

    public async Task<bool> DeleteMessageAsync(Guid conversationId, Guid messageId, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        await RequireConversationAsync(organizationId, userId, conversationId, ct);
        var message = await store.GetMessageAsync(organizationId, conversationId, messageId, ct);
        if (message is null || message.DeletedAt != null) return false;
        message.MarkDeleted(userId, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationId, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        var conversation = await RequireConversationAsync(organizationId, userId, conversationId, ct);
        conversation.MarkDeleted(userId, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return true;
    }

    private async Task<IReadOnlyList<ChatMessageDto>> VisibleMessagesAsync(Guid organizationId, Guid userId, IReadOnlyList<Message> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return Array.Empty<ChatMessageDto>();
        var ids = rows.Select(x => x.Id).ToArray();
        var turns = await store.GetTurnsForMessagesAsync(organizationId, ids, ct);
        var citations = await store.GetCitationsAsync(organizationId, ids, ct);
        var accessible = await retrievalStore.GetAccessibleChunkSourcesAsync(organizationId, userId, clock.UtcNow,
            citations.Where(x => x.ChunkId.HasValue).Select(x => x.ChunkId!.Value).Distinct().ToArray(), ct);
        var sourceById = accessible.ToDictionary(x => x.ChunkId);
        var result = new List<ChatMessageDto>(rows.Count);
        foreach (var row in rows)
        {
            var ownCitations = citations.Where(x => x.AssistantMessageId == row.Id).ToArray();
            if (ownCitations.Any(x => !x.ChunkId.HasValue || !sourceById.ContainsKey(x.ChunkId.Value))) continue;
            var turn = turns.FirstOrDefault(x => x.RequestMessageId == row.Id || x.ResponseMessageId == row.Id);
            var sources = ownCitations.Select(x =>
            {
                var chunkId = x.ChunkId!.Value;
                var source = sourceById[chunkId];
                return new ChatSourceDto(source.DocumentId, chunkId, source.FileName, x.Score ?? 0,
                    x.PageNo, x.CharStart, x.CharEnd);
            }).ToArray();
            result.Add(new ChatMessageDto(row.Id, row.ConversationId, row.SenderType, cipher.Decrypt(row.ContentCiphertext),
                row.SequenceNo, row.CreatedAt, row.SenderType == "user" ? turn?.Status : null,
                row.SenderType == "assistant" ? turn?.ModelProvider : null,
                row.SenderType == "assistant" ? turn?.ModelName : null, sources));
        }
        return result;
    }
}
