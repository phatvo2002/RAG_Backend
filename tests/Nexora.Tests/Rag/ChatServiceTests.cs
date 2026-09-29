using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Rag;
using Nexora.Infrastructure.Options;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Rag;
using Nexora.Domain.Organizations;
using Nexora.Domain.Security;

namespace Nexora.Tests.Rag;

public sealed class ChatServiceTests
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
    }

    [Fact]
    public async Task User_without_active_organization_membership_cannot_start_chat()
    {
        using var db = CreateDatabase();
        var service = CreateService(db, Guid.NewGuid(), new FakeAccess());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.StartTurnAsync(null, "Question", "gemini", Guid.NewGuid(), default));
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
        var history = await service.PromptHistoryAsync(second.ConversationId, second.RequestMessageId, default);
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
        access.Allowed[source.ChunkId] = new RetrievalChunkSource(source.DocumentId, source.ChunkId, source.FileName, 2, null, null, "bucket", "key");
        await service.CompleteTurnAsync(first, "The director is named in the report.", [source], false, default);
        Assert.Equal(2, (await service.ListMessagesAsync(first.ConversationId, null, default)).Count);

        access.Allowed.Clear();
        var visible = await service.ListMessagesAsync(first.ConversationId, null, default);
        Assert.Single(visible);
        Assert.Equal("user", visible[0].Role);
        var second = await service.StartTurnAsync(first.ConversationId, "Any update?", "claude", Guid.NewGuid(), default);
        Assert.Equal("Gemini", db.AiTurns.IgnoreQueryFilters().Single(x => x.Id == first.TurnId).ModelProvider);
        Assert.Equal("Claude", db.AiTurns.IgnoreQueryFilters().Single(x => x.Id == second.TurnId).ModelProvider);
        var history = await service.PromptHistoryAsync(first.ConversationId, second.RequestMessageId, default);
        Assert.DoesNotContain(history, x => x.Content.Contains("director is named"));
    }

    [Fact]
    public async Task Revoked_source_before_completion_prevents_persisting_answer()
    {
        using var db = CreateDatabase();
        var service = CreateService(db, OwnerId, new FakeAccess());
        var turn = await service.StartTurnAsync(null, "Question", "gemini", Guid.NewGuid(), default);
        var source = new RetrievedChunkResponse(Guid.NewGuid(), Guid.NewGuid(), "file.pdf", "excerpt", 0.9f, 1, null, null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CompleteTurnAsync(turn, "Answer", [source], false, default));
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

    private static ChatService CreateService(NexoraDbContext db, Guid userId, FakeAccess access)
    {
        var options = Options.Create(new ChatOptions
        {
            EncryptionKeyBase64 = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray()),
        });
        return new ChatService(db, new FakeOrganization(), new FakeUser(userId), access, new FixedClock(),
            new ChatCipher(options), new ChatModelStreamer(new HttpClient(), options));
    }

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
        public Task<IReadOnlyList<Guid>> GetAccessibleIndexedDocumentIdsAsync(Guid organizationId, Guid userId, DateTimeOffset now, CancellationToken ct) =>
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
