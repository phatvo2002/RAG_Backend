using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nexora.Domain.Organizations;
using Nexora.Domain.Security;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Features.V1.Account;
using Nexora.Application.Features.V1.Account.Model;
using Nexora.Infrastructure.Services.Authentication;
using Nexora.Infrastructure.Services.Identity;
using Nexora.Infrastructure.Services.Rag;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Repositories.Identity;

namespace Nexora.Tests.Rag;

public sealed class ProviderKeyTests
{
    private static readonly Guid FirstUser = Guid.Parse("bf43a680-bcb7-4b38-9f3b-643826680b86");
    private static readonly Guid SecondUser = Guid.Parse("d5f15e99-5134-4e95-bd31-74069b270f8b");

    [Fact]
    public void Key_cipher_uses_durable_authenticated_encryption()
    {
        var options = Options.Create(new ProviderKeyOptions { EncryptionKeyBase64 = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray()) });
        var cipher = new ProviderKeyCipher(options);
        var encrypted = cipher.Encrypt(FirstUser, "gemini", "test-provider-key");
        Assert.Equal("test-provider-key", new ProviderKeyCipher(options).Decrypt(FirstUser, "gemini", encrypted));
        Assert.ThrowsAny<CryptographicException>(() => cipher.Decrypt(SecondUser, "gemini", encrypted));
        Assert.ThrowsAny<CryptographicException>(() => cipher.Decrypt(FirstUser, "claude", encrypted));
        Assert.NotEqual(encrypted, cipher.Encrypt(FirstUser, "gemini", "test-provider-key"));
    }

    [Fact]
    public async Task Users_only_list_and_use_their_own_keys()
    {
        await using var db = Database();
        var first = Service(db, FirstUser);
        var second = Service(db, SecondUser);
        await first.SaveKeyAsync("gemini", "test-first-user-key", null, default);
        var stored = await db.UserProviderKeys.SingleAsync();
        Assert.Equal(32, stored.Sha256.Length);
        Assert.DoesNotContain("test-first-user-key", System.Text.Encoding.UTF8.GetString(stored.Ciphertext));
        Assert.Empty((await second.ListKeysAsync(default)).Keys);
        Assert.Null(await second.GetCredentialAsync("gemini", default));
        var inFlight = (await first.GetCredentialAsync("gemini", default))!;
        Assert.Equal("test-first-user-key", inFlight.ApiKey);
        Assert.False(await second.DeleteKeyAsync("gemini", default));
        Assert.Single((await first.ListKeysAsync(default)).Keys);
        await first.SaveKeyAsync("gemini", "test-replacement-key", null, default);
        Assert.Equal("test-first-user-key", inFlight.ApiKey);
        Assert.Single((await first.ListKeysAsync(default)).Keys);
        Assert.Equal("test-replacement-key", (await first.GetCredentialAsync("gemini", default))!.ApiKey);
        Assert.True(await first.DeleteKeyAsync("gemini", default));
        Assert.Null(await first.GetCredentialAsync("gemini", default));
    }

    [Fact]
    public async Task Claude_rejects_unapproved_gateway()
    {
        await using var db = Database();
        var service = Service(db, FirstUser);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveKeyAsync("claude", "test-provider-key", "unapproved", default));
    }

    [Fact]
    public async Task Claude_uses_only_an_approved_gateway()
    {
        await using var db = Database();
        var options = Options.Create(new ProviderKeyOptions
        {
            EncryptionKeyBase64 = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray()),
            ClaudeEndpoints = new Dictionary<string, string> { ["gateway"] = "https://approved.example/v1" }
        });
        var service = new AccountSettingsService(db, new TestUser(FirstUser), new TestClock(), new ProviderKeyCipher(options), options);
        await service.SaveKeyAsync("claude", "test-claude-key", "gateway", default);
        Assert.Equal("https://approved.example/v1", (await service.GetCredentialAsync("claude", default))!.BaseUrl);
    }

    [Fact]
    public async Task Profile_changes_only_the_current_users_preferences()
    {
        await using var db = Database();
        db.Users.AddRange(
            new ApplicationUser { Id = FirstUser, UserName = "first", Email = "first@example.com" },
            new ApplicationUser { Id = SecondUser, UserName = "second", Email = "second@example.com" });
        await db.SaveChangesAsync();
        var first = Service(db, FirstUser);
        var second = Service(db, SecondUser);
        var updated = await first.UpdateProfileAsync("New name", "vi", "Asia/Ho_Chi_Minh", default);
        Assert.Equal("New name", updated.DisplayName);
        Assert.Equal("first@example.com", updated.Email);
        Assert.Equal("second", (await second.GetProfileAsync(default)).DisplayName);
    }

    [Fact]
    public void Models_require_the_corresponding_provider_key()
    {
        var models = new ChatModelStreamer(new HttpClient(), Options.Create(new ChatOptions()));
        Assert.Empty(models.AvailableModels(false, false));
        Assert.All(models.AvailableModels(true, false), model => Assert.Equal("Gemini", model.Provider));
        Assert.Single(models.AvailableModels(false, true));
    }

    private static NexoraDbContext Database() => new(new DbContextOptionsBuilder<NexoraDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ReplaceService<IModelCustomizer, ProviderKeyTestModelCustomizer>().Options);

    private static AccountSettingsService Service(NexoraDbContext db, Guid userId)
    {
        var options = Options.Create(new ProviderKeyOptions
        {
            EncryptionKeyBase64 = Convert.ToBase64String(Enumerable.Range(0, 32).Select(x => (byte)x).ToArray())
        });
        return new AccountSettingsService(db, new TestUser(userId), new TestClock(), new ProviderKeyCipher(options), options);
    }

    private sealed class TestUser(Guid id) : IUserContext
    {
        public Guid? UserId => id;
        public bool IsAuthenticated => true;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 29, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class ProviderKeyTestModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            modelBuilder.Entity<OrganizationSetting>().Ignore(x => x.SettingsJson);
            modelBuilder.Entity<ActivityLog>().Ignore(x => x.MetadataJson);
        }
    }
}

internal sealed class AccountSettingsService
{
    private readonly AccountSettingsStore store;
    private readonly IUserContext user;
    private readonly IClock clock;
    private readonly ProviderKeyCipher cipher;
    private readonly ProviderKeyOptions options;

    public AccountSettingsService(NexoraDbContext db, IUserContext user, IClock clock, ProviderKeyCipher cipher, IOptions<ProviderKeyOptions> options)
    {
        store = new AccountSettingsStore(db);
        this.user = user;
        this.clock = clock;
        this.cipher = cipher;
        this.options = options.Value;
    }

    public Task<AccountProfileDto> GetProfileAsync(CancellationToken ct) =>
        new GetAccountProfileQueryHandler(store, user).Handle(new GetAccountProfileQuery(), ct);
    public Task<AccountProfileDto> UpdateProfileAsync(string name, string language, string? zone, CancellationToken ct) =>
        new UpdateAccountProfileCommandHandler(store, user, clock).Handle(new UpdateAccountProfileCommand(name, language, zone), ct);
    public Task<ProviderKeySettingsDto> ListKeysAsync(CancellationToken ct) =>
        new GetProviderKeysQueryHandler(store, user, cipher, options).Handle(new GetProviderKeysQuery(), ct);
    public Task<ProviderKeyStatusDto?> SaveKeyAsync(string provider, string key, string? endpoint, CancellationToken ct) =>
        new SaveProviderKeyCommandHandler(store, user, cipher, options, clock).Handle(new SaveProviderKeyCommand(provider, key, endpoint), ct);
    public Task<bool> DeleteKeyAsync(string provider, CancellationToken ct) =>
        new DeleteProviderKeyCommandHandler(store, user).Handle(new DeleteProviderKeyCommand(provider), ct);
    public Task<Nexora.Application.Features.V1.Chat.Model.ProviderCredential?> GetCredentialAsync(string provider, CancellationToken ct) =>
        new GetProviderCredentialQueryHandler(store, user, cipher, options, clock).Handle(new GetProviderCredentialQuery(provider), ct);
}
