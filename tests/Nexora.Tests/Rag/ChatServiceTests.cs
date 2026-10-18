using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Application.Features.V1.Chat;
using Nexora.Application.Features.V1.Chat.Query;
using Nexora.Application.Features.V1.Chat.Command;
using Nexora.Application.Features.V1.Rag.Retrieval.Model;
using Nexora.Infrastructure.Services.Rag;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Repositories.Chat;
using Nexora.Domain.Organizations;
using Nexora.Domain.Security;

namespace Nexora.Tests.Rag;

public sealed class ChatStoreTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public async Task History_survives_a_new_service_and_cipher_instance()
    {
        using var db = CreateDatabase();
        var access = new FakeAccess();
        var first = CreateService(db, OwnerId, access);
        var turn = await first.StartTurnAsync(null, "What is in the report?", "gemini", Guid.NewGuid(), default);
        await first.CompleteTurnAsync(turn, "No supporting document found.", [], true, default);

        var second = CreateService(db, OwnerId, access);
        var conversations = await second.ListConversationsAsync(0, default);
        var messages = await second.ListMessagesAsync(turn.ConversationId, null, default);
        Assert.Single(conversations);
        Assert.Equal("What is in the report?", conversations[0].Title);
        Assert.Equal(new[] { "user", "assistant" }, messages.Select(x => x.Role));
        Assert.Equal("No supporting document found.", messages[1].Content);
        Assert.DoesNotContain("What is in the report?", System.Text.Encoding.UTF8.GetString(db.Messages.IgnoreQueryFilters().Single(x => x.Id == turn.RequestMessageId).ContentCiphertext));
    }

    [Fact]
    public async Task Other_user_cannot_read_or_delete_a_conversation()
    {
        using var db = CreateDatabase();
        var access = new FakeAccess();
        var turn = await CreateService(db, OwnerId, access).StartTurnAsync(null, "Question", "gemini", Guid.NewGuid(), default);
        var outsiderId = Guid.NewGuid();
        AddMember(db, outsiderId);
        var outsider = CreateService(db, outsiderId, access);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => outsider.ListMessagesAsync(turn.ConversationId, null, default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => outsider.DeleteMessageAsync(turn.ConversationId, turn.RequestMessageId, default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => outsider.DeleteConversationAsync(turn.ConversationId, default));
    }

    [Fact]
    public async Task Deleted_conversation_disappears_from_history_and_cannot_be_opened_again()
    {
        using var db = CreateDatabase();
        var service = CreateService(db, OwnerId, new FakeAccess());
        var turn = await service.StartTurnAsync(null, "Private question", "gemini", Guid.NewGuid(), default);

        Assert.True(await service.DeleteConversationAsync(turn.ConversationId, default));
        Assert.Empty(await service.ListConversationsAsync(0, default));
        Assert.NotNull(db.Conversations.IgnoreQueryFilters().Single(x => x.Id == turn.ConversationId).DeletedAt);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ListMessagesAsync(turn.ConversationId, null, default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.StartTurnAsync(turn.ConversationId, "Follow up", "gemini", Guid.NewGuid(), default));
    }

    [Fact]
    public async Task User_without_active_organization_membership_can_start_chat()
    {
        using var db = CreateDatabase();
        var service = CreateService(db, Guid.NewGuid(), new FakeAccess());
        var turn = await service.StartTurnAsync(null, "Question", "gemini", Guid.NewGuid(), default);

        Assert.NotEqual(Guid.Empty, turn.ConversationId);
    }

    [Fact]
    public async Task Deleted_message_is_hidden_and_cannot_enter_follow_up_prompt()
    {
        using var db = CreateDatabase();
        var service = CreateService(db, OwnerId, new FakeAccess());
        var first = await service.StartTurnAsync(null, "Private question", "gemini", Guid.NewGuid(), default);
        await service.CompleteTurnAsync(first, "No source", [], true, default);
        Assert.True(await service.DeleteMessageAsync(first.ConversationId, first.RequestMessageId, default));
        var second = await service.StartTurnAsync(first.ConversationId, "Follow up", "gemini", Guid.NewGuid(), default);
        var history = await PromptHistoryAsync(service, second.ConversationId, second.RequestMessageId);
        Assert.DoesNotContain(history, x => x.Content == "Private question");
        Assert.DoesNotContain(await service.ListMessagesAsync(first.ConversationId, null, default), x => x.Id == first.RequestMessageId);
    }

    [Fact]
    public async Task Revoked_source_hides_saved_answer_and_excludes_it_from_prompt()
    {
        using var db = CreateDatabase();
        var access = new FakeAccess();
        var service = CreateService(db, OwnerId, access);
        var first = await service.StartTurnAsync(null, "Who is the director?", "gemini", Guid.NewGuid(), default);
        var source = new RetrievedChunkResponse(Guid.NewGuid(), Guid.NewGuid(), "report.pdf", "excerpt", 0.83f, 2, null, null);
        access.Allowed[source.ChunkId] = new RetrievalChunkSource(source.DocumentId, source.ChunkId, source.FileName, null, 2, null, null, "bucket", "key");
        await service.CompleteTurnAsync(first, "The director is named in the report.", [source], false, default);
        Assert.Equal(2, (await service.ListMessagesAsync(first.ConversationId, null, default)).Count);

        access.Allowed.Clear();
        var visible = await service.ListMessagesAsync(first.ConversationId, null, default);
        Assert.Single(visible);
        Assert.Equal("user", visible[0].Role);
        var second = await service.StartTurnAsync(first.ConversationId, "Any update?", "claude", Guid.NewGuid(), default);
        Assert.Equal("Gemini", db.AiTurns.IgnoreQueryFilters().Single(x => x.Id == first.TurnId).ModelProvider);
        Assert.Equal("Claude", db.AiTurns.IgnoreQueryFilters().Single(x => x.Id == second.TurnId).ModelProvider);
        var history = await PromptHistoryAsync(service, first.ConversationId, second.RequestMessageId);
        Assert.DoesNotContain(history, x => x.Content.Contains("director is named"));
    }

    [Fact]
    public async Task Revoked_source_before_completion_prevents_persisting_answer()
    {
        using var db = CreateDatabase();
        var access = new FakeAccess();
        var service = CreateService(db, OwnerId, access);
        var turn = await service.StartTurnAsync(null, "Question", "gemini", Guid.NewGuid(), default);
        var source = new RetrievedChunkResponse(Guid.NewGuid(), Guid.NewGuid(), "file.pdf", "excerpt", 0.9f, 1, null, null);
        var handler = new CompleteChatTurnCommandHandler(service, access, new FakeOrganization(), new FakeUser(OwnerId), new FixedClock());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(new CompleteChatTurnCommand(turn, "Answer", [source], false), default));
        Assert.Single(db.Messages.IgnoreQueryFilters());
    }

    [Fact]
    public async Task Duplicate_request_id_does_not_create_another_message()
    {
        using var db = CreateDatabase();
        var service = CreateService(db, OwnerId, new FakeAccess());
        var requestId = Guid.NewGuid();
        await service.StartTurnAsync(null, "Question", "gemini", requestId, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartTurnAsync(null, "Question", "gemini", requestId, default));
        Assert.Single(db.Messages.IgnoreQueryFilters());
    }

    [Fact]
    public async Task Completed_turn_records_the_model_version_that_succeeded()
    {
        using var db = CreateDatabase();
        var service = CreateService(db, OwnerId, new FakeAccess());
        var turn = await service.StartTurnAsync(null, "Question", "gemini", Guid.NewGuid(), default);
        var fallbackModel = new ChatModelChoice("gemini-3.6-flash", "Gemini 3.6 Flash", "Gemini", "gemini-3.6-flash");

        var message = await service.CompleteTurnAsync(turn with { Model = fallbackModel }, "Answer", [], true, default);

        var savedTurn = db.AiTurns.IgnoreQueryFilters().Single(x => x.Id == turn.TurnId);
        Assert.Equal("gemini-3.6-flash", savedTurn.ModelName);
        Assert.Equal("Gemini", savedTurn.ModelProvider);
        Assert.Equal("gemini-3.6-flash", message.ModelName);
        Assert.Equal("Gemini", message.ModelProvider);
    }

    private static NexoraDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<NexoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCustomizer, ChatTestModelCustomizer>()
            .Options;
        var db = new NexoraDbContext(options, clock: new FixedClock());
        AddMember(db, OwnerId);
        return db;
    }

    private static void AddMember(NexoraDbContext db, Guid userId)
    {
        var member = OrganizationMembership.Create(OrganizationId, userId, OwnerId, new FixedClock().UtcNow);
        member.Activate(new FixedClock().UtcNow);
        db.OrganizationMemberships.Add(member);
        db.SaveChanges();
    }

    private static ChatWorkflow CreateService(NexoraDbContext db, Guid userId, FakeAccess access)
    {
        var options = Options.Create(new ChatOptions
        {
            EncryptionKeyBase64 = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray()),
        });
        return new ChatWorkflow(new ChatStore(db), new FakeOrganization(), new FakeUser(userId), access, new FixedClock(),
            new ChatCipher(options), new ChatModelStreamer(new HttpClient(), options));
    }

    private static Task<IReadOnlyList<ChatPromptMessage>> PromptHistoryAsync(ChatWorkflow store, Guid conversationId, Guid requestMessageId) =>
        new GetChatPromptHistoryQueryHandler(store).Handle(new GetChatPromptHistoryQuery(conversationId, requestMessageId), default);

    private sealed class FakeOrganization : IDefaultOrganizationProvider
    {
        public Task<Guid?> GetOrganizationIdAsync(CancellationToken ct) => Task.FromResult<Guid?>(OrganizationId);
    }

    private sealed class FakeUser(Guid id) : IUserContext
    {
        public Guid? UserId => id;
        public bool IsAuthenticated => true;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 28, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeAccess : IDocumentRetrievalStore
    {
        public Dictionary<Guid, RetrievalChunkSource> Allowed { get; } = [];
        public Task<string?> GetOrganizationNameAsync(Guid organizationId, CancellationToken ct) => Task.FromResult<string?>("Nexora");
        public Task<IReadOnlyList<IndexedDocumentReference>> GetIndexedDocumentsAsync(Guid organizationId, string indexName, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<IndexedDocumentReference>>(Allowed.Values.Select(x => new IndexedDocumentReference(x.DocumentId, x.DepartmentName is not null)).Distinct().ToArray());
        public Task<IReadOnlyList<Guid>> GetAccessibleIndexedDocumentIdsAsync(Guid organizationId, Guid userId, DateTimeOffset now, string indexName, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Guid>>(Allowed.Values.Select(x => x.DocumentId).Distinct().ToArray());
        public Task<IReadOnlyList<RetrievalChunkSource>> GetAccessibleChunkSourcesAsync(Guid organizationId, Guid userId, DateTimeOffset now,
            IReadOnlyCollection<Guid> chunkIds, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RetrievalChunkSource>>(chunkIds.Where(Allowed.ContainsKey).Select(id => Allowed[id]).ToArray());
    }

    private sealed class ChatTestModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            modelBuilder.Entity<OrganizationSetting>().Ignore(x => x.SettingsJson);
            modelBuilder.Entity<ActivityLog>().Ignore(x => x.MetadataJson);
        }
    }
}
