using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.Documents.Query;
using Nexora.Domain.Documents;
using Nexora.Domain.Organizations;
using Nexora.Domain.Security;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Repositories.Documents;

namespace Nexora.Tests.Documents;

public sealed class GetDocumentChunksQueryTests
{
    [Fact]
    public async Task Repository_applies_document_access_and_returns_current_version_in_ordinal_order()
    {
        var options = new DbContextOptionsBuilder<NexoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCustomizer, TestModelCustomizer>()
            .Options;
        await using var db = new NexoraDbContext(options, clock: new TestClock());
        var organizationId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var departmentId = Guid.NewGuid();
        var folder = DocumentFolder.Create(organizationId, departmentId, ownerId, "Folder", now);
        var document = Document.Create(organizationId, departmentId, ownerId, "Document", now);
        var version = DocumentVersion.Create(organizationId, document.Id, 1, "file.txt", "text/plain", 100,
            "minio", "test", "source", new byte[32], ownerId, now);
        document.SetFolder(folder.Id);
        document.SetCurrentVersion(version.Id);
        db.DocumentFolders.Add(folder);
        db.Documents.Add(document);
        db.DocumentVersions.Add(version);
        foreach (var ordinal in new[] { 2, 0, 1 })
        {
            var hash = ordinal.ToString("x").PadLeft(64, '0');
            var chunk = DocumentChunk.Create(organizationId, version.Id, ordinal, hash, now,
                charStart: ordinal * 10, charEnd: ordinal * 10 + 5);
            db.DocumentChunks.Add(chunk);
            db.DocumentChunkContents.Add(DocumentChunkContent.Create(chunk.Id, "minio", "test", $"chunk-{ordinal}", hash, now));
        }
        await db.SaveChangesAsync();
        var repository = new DocumentRepository(db);

        var firstPage = await repository.GetAccessiblePageAsync(organizationId, ownerId, now, document.Id, 1, 2, default);
        Assert.NotNull(firstPage);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal([0, 1], firstPage.Items.Select(chunk => chunk.Ordinal));
        var secondPage = await repository.GetAccessiblePageAsync(organizationId, ownerId, now, document.Id, 2, 2, default);
        Assert.Equal([2], secondPage!.Items.Select(chunk => chunk.Ordinal));
        Assert.Null(await repository.GetAccessiblePageAsync(organizationId, outsiderId, now, document.Id, 1, 2, default));
        Assert.Null(await repository.GetAccessiblePageAsync(Guid.NewGuid(), ownerId, now, document.Id, 1, 2, default));

        var grant = DocumentFolderAccessGrant.Create(organizationId, folder.Id, null, viewerId, "READ", ownerId, now.AddMinutes(-1));
        db.DocumentFolderAccessGrants.Add(grant);
        await db.SaveChangesAsync();
        Assert.NotNull(await repository.GetAccessiblePageAsync(organizationId, viewerId, now, document.Id, 1, 2, default));
        grant.Revoke(ownerId, now);
        await db.SaveChangesAsync();
        Assert.Null(await repository.GetAccessiblePageAsync(organizationId, viewerId, now, document.Id, 1, 2, default));

        document.MarkDeleted(ownerId, now);
        await db.SaveChangesAsync();
        Assert.Null(await repository.GetAccessiblePageAsync(organizationId, ownerId, now, document.Id, 1, 2, default));
    }

    [Fact]
    public async Task Handler_reads_only_authorized_page_and_reports_missing_storage()
    {
        var record = new DocumentChunkReadRecord(Guid.NewGuid(), 0, null, 0, 5, "vie", "test", "chunk-0");
        var store = new FakeStore(new PagedResult<DocumentChunkReadRecord>([record], 1, 25, 1));
        var storage = new FakeStorage();
        var handler = new GetDocumentChunksQueryHandler(store, new TestOrganizationProvider(),
            new TestUserContext(), new TestClock(), storage);

        var result = await handler.Handle(new GetDocumentChunksQuery(Guid.NewGuid()), default);
        Assert.True(result.IsSuccess);
        Assert.Equal("content", Assert.Single(result.Value!.Items).Text);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(1, storage.ReadCount);

        store.Page = null;
        var denied = await handler.Handle(new GetDocumentChunksQuery(Guid.NewGuid()), default);
        Assert.False(denied.IsSuccess);
        Assert.Equal(1, storage.ReadCount);

        store.Page = new PagedResult<DocumentChunkReadRecord>([record], 1, 25, 1);
        storage.Fail = true;
        await Assert.ThrowsAsync<FileNotFoundException>(() => handler.Handle(new GetDocumentChunksQuery(Guid.NewGuid()), default));
    }

    private sealed class FakeStore(PagedResult<DocumentChunkReadRecord>? page) : IDocumentChunkReadStore
    {
        public PagedResult<DocumentChunkReadRecord>? Page { get; set; } = page;
        public Task<PagedResult<DocumentChunkReadRecord>?> GetAccessiblePageAsync(Guid organizationId, Guid userId,
            DateTimeOffset now, Guid documentId, int pageNumber, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(Page);
    }

    private sealed class FakeStorage : IObjectStorage
    {
        public int ReadCount { get; private set; }
        public bool Fail { get; set; }
        public Task<Stream> GetAsync(string bucket, string objectKey, CancellationToken cancellationToken)
        {
            ReadCount++;
            if (Fail) throw new FileNotFoundException();
            return Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content")));
        }
        public Task PutAsync(string bucket, string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class TestOrganizationProvider : IDefaultOrganizationProvider
    {
        public Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken) => Task.FromResult<Guid?>(Guid.NewGuid());
    }
    private sealed class TestUserContext : IUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => true;
    }
    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
    private sealed class TestModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            modelBuilder.Entity<OrganizationSetting>().Ignore(item => item.SettingsJson);
            modelBuilder.Entity<ActivityLog>().Ignore(item => item.MetadataJson);
        }
    }
}
