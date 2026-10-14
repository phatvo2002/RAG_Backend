using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Permissions;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Organizations;
using Nexora.Infrastructure.Services.Authorization;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Tests.WebApi;

public sealed class ApiPermissionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Active_catalog_entry_is_allowed_without_organization_or_role_grant()
    {
        await using var context = await CreateContextAsync();

        var hasAccess = await context.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/turns/stream"),
            CancellationToken.None);

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task Request_without_matching_active_catalog_entry_is_denied()
    {
        await using var context = await CreateContextAsync();

        var hasAccess = await context.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/other"),
            CancellationToken.None);

        Assert.False(hasAccess);
    }

    private static async Task<TestContext> CreateContextAsync()
    {
        var userContext = new TestUserContext();
        var clock = new TestClock();
        var options = new DbContextOptionsBuilder<NexoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCustomizer, TestModelCustomizer>()
            .Options;
        var db = new NexoraDbContext(options, userContext, clock: clock);
        db.ApiFunctions.Add(ApiFunction.Create(
            "chat.stream",
            "Chat",
            "Stream chat answer",
            ApiHttpMethod.Post,
            "/api/chat/turns/stream",
            ApiActionType.Write,
            ApiRiskLevel.Medium,
            Now.AddDays(-1)));
        await db.SaveChangesAsync();

        return new TestContext(db, new ApiPermissionService(db));
    }

    private sealed record TestContext(NexoraDbContext Db, ApiPermissionService Service) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class TestUserContext : IUserContext
    {
        public Guid? UserId => null;
        public bool IsAuthenticated => false;
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class TestModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            modelBuilder.Entity<OrganizationSetting>().Ignore(x => x.SettingsJson);
            modelBuilder.Entity<Nexora.Domain.Security.ActivityLog>().Ignore(x => x.MetadataJson);
        }
    }
}
