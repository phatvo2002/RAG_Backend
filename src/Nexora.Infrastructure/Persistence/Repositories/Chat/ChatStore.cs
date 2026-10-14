using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Domain.Chat;

namespace Nexora.Infrastructure.Persistence.Repositories.Chat;

internal sealed class ChatStore(NexoraDbContext db) : IChatStore
{
    public async Task<IReadOnlyList<Conversation>> ListConversationsAsync(Guid organizationId, Guid userId, int skip, CancellationToken cancellationToken) =>
        await db.Conversations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.OwnerUserId == userId && x.DeletedAt == null)
            .OrderByDescending(x => x.UpdatedAt).Skip(Math.Clamp(skip, 0, 10000)).Take(50).ToListAsync(cancellationToken);

    public Task<Conversation?> GetConversationAsync(Guid organizationId, Guid userId, Guid conversationId, CancellationToken cancellationToken) =>
        db.Conversations.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.OrganizationId == organizationId &&
            x.OwnerUserId == userId && x.Id == conversationId && x.DeletedAt == null, cancellationToken);

    public Task<bool> RequestExistsAsync(Guid organizationId, Guid clientRequestId, CancellationToken cancellationToken) =>
        db.AiTurns.IgnoreQueryFilters().AnyAsync(x => x.OrganizationId == organizationId && x.CorrelationId == clientRequestId.ToString(), cancellationToken);

    public async Task<int> NextSequenceAsync(Guid organizationId, Guid conversationId, CancellationToken cancellationToken) =>
        await db.Messages.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.ConversationId == conversationId)
            .MaxAsync(x => (int?)x.SequenceNo, cancellationToken) ?? 0;

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(Guid organizationId, Guid conversationId, int? beforeSequenceNo, Guid? excludeMessageId, int limit, CancellationToken cancellationToken)
    {
        var query = db.Messages.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.ConversationId == conversationId && x.DeletedAt == null);
        if (beforeSequenceNo.HasValue) query = query.Where(x => x.SequenceNo < beforeSequenceNo.Value);
        if (excludeMessageId.HasValue) query = query.Where(x => x.Id != excludeMessageId.Value);
        return await query.OrderByDescending(x => x.SequenceNo).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiTurn>> GetStaleTurnsAsync(Guid organizationId, Guid conversationId, DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        await db.AiTurns.IgnoreQueryFilters().Where(x => x.OrganizationId == organizationId && x.ConversationId == conversationId &&
            x.Status == "PENDING" && x.StartedAt < cutoff).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AiTurn>> GetTurnsForMessagesAsync(Guid organizationId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken) =>
        await db.AiTurns.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && (messageIds.Contains(x.RequestMessageId) ||
                (x.ResponseMessageId.HasValue && messageIds.Contains(x.ResponseMessageId.Value))))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Citation>> GetCitationsAsync(Guid organizationId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken) =>
        await db.Citations.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && messageIds.Contains(x.AssistantMessageId))
            .OrderBy(x => x.CitationNo).ToListAsync(cancellationToken);

    public Task<AiTurn?> GetTurnAsync(Guid turnId, Guid? organizationId, Guid? conversationId, CancellationToken cancellationToken) =>
        db.AiTurns.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == turnId &&
            (!organizationId.HasValue || x.OrganizationId == organizationId.Value) &&
            (!conversationId.HasValue || x.ConversationId == conversationId.Value), cancellationToken);

    public Task<Message?> GetMessageAsync(Guid organizationId, Guid conversationId, Guid messageId, CancellationToken cancellationToken) =>
        db.Messages.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.ConversationId == conversationId && x.Id == messageId, cancellationToken);

    public void AddConversation(Conversation conversation) => db.Conversations.Add(conversation);
    public void AddMessage(Message message) => db.Messages.Add(message);
    public void AddTurn(AiTurn turn) => db.AiTurns.Add(turn);
    public void AddCitation(Citation citation) => db.Citations.Add(citation);
    public void SetConversationUpdatedAt(Conversation conversation, DateTimeOffset now) => db.Entry(conversation).Property(x => x.UpdatedAt).CurrentValue = now;
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);
}
