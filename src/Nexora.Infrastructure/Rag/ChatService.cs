using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;
using Nexora.Domain.Chat;
using Nexora.Domain.Organizations.Enums;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Rag;

public sealed class ChatService(
    NexoraDbContext db,
    IDefaultOrganizationProvider organizationProvider,
    IUserContext userContext,
    IDocumentRetrievalStore retrievalStore,
    IClock clock,
    ChatCipher cipher,
    ChatModelStreamer models) : IChatService
{
    private async Task<(Guid OrganizationId, Guid UserId)> IdentityAsync(CancellationToken ct)
    {
        var organizationId = await organizationProvider.GetOrganizationIdAsync(ct);
        if (!organizationId.HasValue || !userContext.UserId.HasValue)
            throw new UnauthorizedAccessException("An organization and signed-in user are required.");
        var isMember = await db.OrganizationMemberships.IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
            x.OrganizationId == organizationId.Value && x.UserId == userContext.UserId.Value &&
            x.Status == OrganizationMembershipStatus.Active, ct);
        // if (!isMember) throw new UnauthorizedAccessException("Active organization membership is required for chat.");
        return (organizationId.Value, userContext.UserId.Value);
    }

    public async Task<IReadOnlyList<ChatConversationDto>> ListConversationsAsync(int skip, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        var rows = await db.Conversations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.OwnerUserId == userId && x.DeletedAt == null)
            .OrderByDescending(x => x.UpdatedAt).Skip(Math.Clamp(skip, 0, 10000)).Take(50).ToListAsync(ct);
        return rows.Select(x => new ChatConversationDto(x.Id, x.TitleCiphertext is null ? "Cuộc trò chuyện" : cipher.Decrypt(x.TitleCiphertext), x.UpdatedAt)).ToArray();
    }

    public async Task<IReadOnlyList<ChatMessageDto>> ListMessagesAsync(Guid conversationId, int? beforeSequenceNo, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        await RequireConversationAsync(organizationId, userId, conversationId, ct);
        var now = clock.UtcNow;
        var staleCutoff = now.AddMinutes(-5);
        var staleTurns = await db.AiTurns.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.ConversationId == conversationId &&
            x.Status == "PENDING" && x.StartedAt < staleCutoff)
            .ToListAsync(ct);
        foreach (var turn in staleTurns)
        {
            turn.Status = "FAILED";
            turn.ErrorCode = "INTERRUPTED";
            turn.CompletedAt = now;
        }
        if (staleTurns.Count > 0) await db.SaveChangesAsync(ct);
        var query = db.Messages.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ConversationId == conversationId && x.DeletedAt == null);
        if (beforeSequenceNo.HasValue) query = query.Where(x => x.SequenceNo < beforeSequenceNo.Value);
        var rows = await query.OrderByDescending(x => x.SequenceNo).Take(50).ToListAsync(ct);
        return await VisibleMessagesAsync(organizationId, userId, rows.OrderBy(x => x.SequenceNo).ToArray(), ct);
    }

    public async Task<IReadOnlyList<ChatPromptMessage>> PromptHistoryAsync(Guid conversationId, Guid requestMessageId, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        await RequireConversationAsync(organizationId, userId, conversationId, ct);
        var rows = await db.Messages.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ConversationId == conversationId && x.DeletedAt == null && x.Id != requestMessageId)
            .OrderByDescending(x => x.SequenceNo).Take(100).ToListAsync(ct);
        var visible = await VisibleMessagesAsync(organizationId, userId, rows.OrderBy(x => x.SequenceNo).ToArray(), ct);
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

    public async Task<ChatTurnStart> StartTurnAsync(Guid? conversationId, string question, string modelId, Guid clientRequestId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > 2000 || clientRequestId == Guid.Empty)
            throw new ArgumentException("A question of at most 2000 characters and a request ID are required.");
        var model = models.Resolve(modelId);
        var (organizationId, userId) = await IdentityAsync(ct);
        if (await db.AiTurns.IgnoreQueryFilters().AnyAsync(x => x.OrganizationId == organizationId && x.CorrelationId == clientRequestId.ToString(), ct))
            throw new InvalidOperationException("This chat request was already submitted.");

        Conversation conversation;
        if (conversationId.HasValue)
        {
            conversation = await RequireConversationAsync(organizationId, userId, conversationId.Value, ct);
        }
        else
        {
            conversation = Conversation.Create(Guid.NewGuid(), organizationId, userId,
                cipher.Encrypt(question.Trim()[..Math.Min(question.Trim().Length, 80)]), clock.UtcNow);
            db.Conversations.Add(conversation);
        }

        var nextSequence = await db.Messages.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.ConversationId == conversation.Id)
            .MaxAsync(x => (int?)x.SequenceNo, ct) ?? 0;
        var message = Message.Create(Guid.NewGuid(), organizationId, conversation.Id, "user", userId,
            cipher.Encrypt(question.Trim()), ChatCipher.Hash(question.Trim()), nextSequence + 1, clock.UtcNow);
        var turn = AiTurn.Create(Guid.NewGuid(), organizationId, conversation.Id, message.Id,
            clientRequestId.ToString(), model.Provider, model.Model, RagContextComposer.PromptVersion, clock.UtcNow);
        db.Messages.Add(message);
        db.AiTurns.Add(turn);
        await db.SaveChangesAsync(ct);
        return new ChatTurnStart(conversation.Id, message.Id, turn.Id, model);
    }

    public async Task<ChatMessageDto> CompleteTurnAsync(ChatTurnStart start, string answer,
        IReadOnlyList<RetrievedChunkResponse> sources, bool noSource, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        var conversation = await RequireConversationAsync(organizationId, userId, start.ConversationId, ct);
        if (sources.Count > 0)
        {
            var accessible = await retrievalStore.GetAccessibleChunkSourcesAsync(organizationId, userId, clock.UtcNow,
                sources.Select(x => x.ChunkId).ToArray(), ct);
            var allowed = accessible.ToDictionary(x => x.ChunkId);
            if (sources.Any(x => !allowed.TryGetValue(x.ChunkId, out var current) || current.DocumentId != x.DocumentId))
                throw new UnauthorizedAccessException("A supporting document is no longer accessible.");
        }
        var turn = await db.AiTurns.IgnoreQueryFilters().SingleAsync(x => x.OrganizationId == organizationId && x.Id == start.TurnId, ct);
        var nextSequence = await db.Messages.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.ConversationId == start.ConversationId)
            .MaxAsync(x => (int?)x.SequenceNo, ct) ?? 0;
        var message = Message.Create(Guid.NewGuid(), organizationId, start.ConversationId, "assistant", null,
            cipher.Encrypt(answer), ChatCipher.Hash(answer), nextSequence + 1, clock.UtcNow);
        db.Messages.Add(message);
        foreach (var source in sources.Select((value, index) => (value, index)))
            db.Citations.Add(Citation.Create(Guid.NewGuid(), organizationId, message.Id, source.value.ChunkId,
                source.index + 1, source.value.PageNo, source.value.CharStart, source.value.CharEnd, source.value.Score, clock.UtcNow));
        turn.ModelProvider = start.Model.Provider;
        turn.ModelName = start.Model.Model;
        if (noSource) turn.NoSourceReason = "NO_AUTHORIZED_SOURCE";
        turn.Complete(message.Id, clock.UtcNow);
        db.Entry(conversation).Property(x => x.UpdatedAt).CurrentValue = clock.UtcNow;
        await db.SaveChangesAsync(ct);
        return new ChatMessageDto(message.Id, start.ConversationId, "assistant", answer, message.SequenceNo, message.CreatedAt,
            turn.Status, start.Model.Provider, start.Model.Model, sources.Select(x => new ChatSourceDto(x.DocumentId, x.ChunkId, x.FileName, x.Score, x.PageNo, x.CharStart, x.CharEnd)).ToArray());
    }

    public async Task FailTurnAsync(ChatTurnStart start, string code)
    {
        var turn = await db.AiTurns.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == start.TurnId && x.ConversationId == start.ConversationId);
        if (turn is null || turn.Status == "COMPLETED") return;
        turn.Status = "FAILED";
        turn.ErrorCode = code;
        turn.CompletedAt = clock.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteMessageAsync(Guid conversationId, Guid messageId, CancellationToken ct)
    {
        var (organizationId, userId) = await IdentityAsync(ct);
        await RequireConversationAsync(organizationId, userId, conversationId, ct);
        var message = await db.Messages.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.ConversationId == conversationId && x.Id == messageId, ct);
        if (message is null || message.DeletedAt != null) return false;
        message.MarkDeleted(userId, clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<Conversation> RequireConversationAsync(Guid organizationId, Guid userId, Guid conversationId, CancellationToken ct) =>
        await db.Conversations.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.OrganizationId == organizationId &&
            x.OwnerUserId == userId && x.Id == conversationId && x.DeletedAt == null, ct)
        ?? throw new KeyNotFoundException("Conversation was not found.");

    private async Task<IReadOnlyList<ChatMessageDto>> VisibleMessagesAsync(Guid organizationId, Guid userId, IReadOnlyList<Message> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return Array.Empty<ChatMessageDto>();
        var ids = rows.Select(x => x.Id).ToArray();
        var turns = await db.AiTurns.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && (ids.Contains(x.RequestMessageId) || (x.ResponseMessageId.HasValue && ids.Contains(x.ResponseMessageId.Value))))
            .ToListAsync(ct);
        var citations = await db.Citations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && ids.Contains(x.AssistantMessageId))
            .OrderBy(x => x.CitationNo).ToListAsync(ct);
        var accessible = await retrievalStore.GetAccessibleChunkSourcesAsync(organizationId, userId, clock.UtcNow,
            citations.Select(x => x.ChunkId).Distinct().ToArray(), ct);
        var sourceById = accessible.ToDictionary(x => x.ChunkId);
        var result = new List<ChatMessageDto>(rows.Count);
        foreach (var row in rows)
        {
            var ownCitations = citations.Where(x => x.AssistantMessageId == row.Id).ToArray();
            if (ownCitations.Any(x => !sourceById.ContainsKey(x.ChunkId))) continue;
            var turn = turns.FirstOrDefault(x => x.RequestMessageId == row.Id || x.ResponseMessageId == row.Id);
            var sources = ownCitations.Select(x =>
            {
                var source = sourceById[x.ChunkId];
                return new ChatSourceDto(source.DocumentId, x.ChunkId, source.FileName, x.Score ?? 0,
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
