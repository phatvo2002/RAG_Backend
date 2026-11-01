using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Permissions;
using Nexora.Domain.Authorization;
using Nexora.Domain.Organizations;
using Nexora.Infrastructure.Services.Authorization;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Repositories;
using Nexora.Infrastructure.Services.Identity;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Tests.WebApi;

public sealed class ApiPermissionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Active_catalog_entry_is_denied_without_role_grant()
    {
        await using var context = await CreateContextAsync(grantFunctions: false);

        var hasAccess = await context.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/turns/stream"),
            CancellationToken.None);

        Assert.False(hasAccess);
    }

    [Fact]
    public async Task Active_catalog_entry_is_allowed_with_effective_role_grant()
    {
        await using var context = await CreateContextAsync();
        Assert.True(await context.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/turns/stream"), CancellationToken.None));
    }

    [Fact]
    public async Task Unauthenticated_or_other_organization_is_denied()
    {
        await using var unauthenticated = await CreateContextAsync(authenticated: false);
        Assert.False(await unauthenticated.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/turns/stream"), CancellationToken.None));
        await using var otherOrganization = await CreateContextAsync(selectedOrganizationId: Guid.NewGuid());
        Assert.False(await otherOrganization.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/turns/stream"), CancellationToken.None));
    }

    [Fact]
    public async Task Deny_or_expired_grant_blocks_access()
    {
        await using var denied = await CreateContextAsync();
        var functionId = denied.Db.ApiFunctions.Single(x => x.Code.Value == "CHAT.STREAM").Id;
        var deniedRoleId = Guid.NewGuid();
        denied.Db.Roles.Add(new ApplicationRole { Id = deniedRoleId, Name = "Blocked", NormalizedName = "BLOCKED" });
        denied.Db.OrganizationRoleAssignments.Add(OrganizationRoleAssignment.Create(
            denied.OrganizationId, denied.UserId, deniedRoleId, denied.UserId, Now.AddDays(-1)));
        denied.Db.RoleApiFunctions.Add(RoleApiFunction.Create(denied.OrganizationId, deniedRoleId,
            functionId, PermissionEffect.Deny, denied.UserId, Now.AddDays(-1)));
        await denied.Db.SaveChangesAsync();
        Assert.False(await denied.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/turns/stream"), CancellationToken.None));

        await using var expired = await CreateContextAsync();
        var grant = expired.Db.RoleApiFunctions.IgnoreQueryFilters().Single(x => x.FunctionId == expired.Db.ApiFunctions.Single(f => f.Code.Value == "CHAT.STREAM").Id);
        grant.Regrant(PermissionEffect.Allow, expired.UserId, Now.AddDays(-2), Now.AddHours(-1));
        await expired.Db.SaveChangesAsync();
        Assert.False(await expired.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/turns/stream"), CancellationToken.None));

        await using var revoked = await CreateContextAsync();
        var revokedGrant = revoked.Db.RoleApiFunctions.IgnoreQueryFilters().Single();
        revokedGrant.Revoke(revoked.UserId, Now);
        await revoked.Db.SaveChangesAsync();
        Assert.False(await revoked.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/chat/turns/stream"), CancellationToken.None));
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

    [Fact]
    public async Task Active_document_chunks_catalog_entry_allows_exact_get_route()
    {
        await using var context = await CreateContextAsync(includeDocumentChunks: true);

        var hasAccess = await context.Service.HasAccessAsync(
            new ApiRequestDescriptor("GET", "/api/documents/{documentId:guid}/chunks"),
            CancellationToken.None);

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task Document_chunks_route_is_denied_when_catalog_entry_is_missing_or_inactive()
    {
        await using var missing = await CreateContextAsync();
        var missingAccess = await missing.Service.HasAccessAsync(
            new ApiRequestDescriptor("GET", "/api/documents/{documentId:guid}/chunks"),
            CancellationToken.None);
        Assert.False(missingAccess);

        await using var inactive = await CreateContextAsync(includeDocumentChunks: true, documentChunksActive: false);
        var inactiveAccess = await inactive.Service.HasAccessAsync(
            new ApiRequestDescriptor("GET", "/api/documents/{documentId:guid}/chunks"),
            CancellationToken.None);
        Assert.False(inactiveAccess);
    }

    [Fact]
    public async Task Document_chunks_catalog_entry_requires_matching_method_and_route()
    {
        await using var context = await CreateContextAsync(includeDocumentChunks: true);

        var wrongMethod = await context.Service.HasAccessAsync(
            new ApiRequestDescriptor("POST", "/api/documents/{documentId:guid}/chunks"),
            CancellationToken.None);
        var wrongRoute = await context.Service.HasAccessAsync(
            new ApiRequestDescriptor("GET", "/api/documents/{documentId:guid}/content"),
            CancellationToken.None);

        Assert.False(wrongMethod);
        Assert.False(wrongRoute);
    }

    [Fact]
    public async Task Active_system_prompt_catalog_entry_allows_exact_read_route()
    {
        await using var context = await CreateContextAsync(includeSystemPrompt: true);
        Assert.True(await context.Service.HasAccessAsync(
            new ApiRequestDescriptor("GET", "/api/system-prompts"), CancellationToken.None));
    }

    [Fact]
    public async Task System_prompt_route_is_denied_when_missing_inactive_or_mismatched()
    {
        await using var missing = await CreateContextAsync();
        Assert.False(await missing.Service.HasAccessAsync(new ApiRequestDescriptor("GET", "/api/system-prompts"), CancellationToken.None));

        await using var inactive = await CreateContextAsync(includeSystemPrompt: true, systemPromptActive: false);
        Assert.False(await inactive.Service.HasAccessAsync(new ApiRequestDescriptor("GET", "/api/system-prompts"), CancellationToken.None));

        await using var active = await CreateContextAsync(includeSystemPrompt: true);
        Assert.False(await active.Service.HasAccessAsync(new ApiRequestDescriptor("PUT", "/api/system-prompts"), CancellationToken.None));
        Assert.False(await active.Service.HasAccessAsync(new ApiRequestDescriptor("GET", "/api/system-prompts/CHAT"), CancellationToken.None));
    }

    private static async Task<TestContext> CreateContextAsync(bool includeDocumentChunks = false, bool documentChunksActive = true,
        bool includeSystemPrompt = false, bool systemPromptActive = true, bool grantFunctions = true,
        bool authenticated = true, Guid? selectedOrganizationId = null)
    {
        var organizationId = Guid.Parse("a47f8ce4-4c35-4744-a842-3e6ee7a81932");
        var userId = Guid.Parse("3eeab4ba-0a13-4baa-b38d-6f25bab02f24");
        var roleId = Guid.NewGuid();
        var userContext = new TestUserContext(authenticated ? userId : null);
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
        if (includeDocumentChunks)
        {
            var documentChunks = ApiFunction.Create(
                "DOCUMENT.CHUNKS.READ",
                "Document",
                "Get document chunks",
                ApiHttpMethod.Get,
                "/api/documents/{documentId:guid}/chunks",
                ApiActionType.Read,
                ApiRiskLevel.Low,
                Now.AddDays(-1));
            if (!documentChunksActive) documentChunks.Deactivate();
            db.ApiFunctions.Add(documentChunks);
        }
        if (includeSystemPrompt)
        {
            var systemPrompt = ApiFunction.Create("SYSTEM_PROMPT.READ", "SystemPrompt", "Read system prompts",
                ApiHttpMethod.Get, "/api/system-prompts", ApiActionType.Read, ApiRiskLevel.Low, Now.AddDays(-1));
            if (!systemPromptActive) systemPrompt.Deactivate();
            db.ApiFunctions.Add(systemPrompt);
        }
        db.Roles.Add(new ApplicationRole { Id = roleId, Name = "Member", NormalizedName = "MEMBER" });
        db.OrganizationRoleAssignments.Add(OrganizationRoleAssignment.Create(organizationId, userId, roleId, userId, Now.AddDays(-1)));
        if (grantFunctions)
            foreach (var function in db.ApiFunctions.Local.ToArray())
                db.RoleApiFunctions.Add(RoleApiFunction.Create(organizationId, roleId, function.Id,
                    PermissionEffect.Allow, userId, Now.AddDays(-1)));
        await db.SaveChangesAsync();

        return new TestContext(db, new ApiPermissionService(new ApiFunctionRepository(db, userContext, clock),
            new TestOrganizationProvider(selectedOrganizationId ?? organizationId), userContext, clock), organizationId, userId, roleId);
    }

    private sealed record TestContext(NexoraDbContext Db, ApiPermissionService Service,
        Guid OrganizationId, Guid UserId, Guid RoleId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class TestUserContext(Guid? userId) : IUserContext
    {
        public Guid? UserId => userId;
        public bool IsAuthenticated => userId.HasValue;
    }

    private sealed class TestOrganizationProvider(Guid? organizationId) : IDefaultOrganizationProvider
    {
        public Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken) => Task.FromResult(organizationId);
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
