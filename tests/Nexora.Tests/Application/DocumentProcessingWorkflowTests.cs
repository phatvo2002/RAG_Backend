using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Features.V1.Documents.Command;
using Nexora.Domain.Documents;
using Nexora.Domain.Chat;
using Nexora.Domain.Rag;
using Nexora.Infrastructure.Services.DocumentProcessing;
using Nexora.Infrastructure.Services.Integrations.Embeddings;
using Nexora.Infrastructure.Services.Integrations.VectorStores;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Repositories.Documents;
using Nexora.Domain.Organizations;
using Nexora.Domain.Security;

namespace Nexora.Tests.Application;

public sealed class DocumentProcessingWorkflowTests
{
    [Theory]
    [InlineData("STOPPING")]
    [InlineData("STOPPED")]
    public async Task Stopped_version_is_not_processed_again(string status)
    {
        var store = new FakeStore(1);
        store.Version.SetProcessingStatus(status);
        var provider = new FakeEmbeddingProvider();
        var vectors = new FakeVectorStore();

        await CreateHandler(store, provider, new FakeScheduler(), vectors)
            .Handle(new ProcessDocumentVersionCommand(store.Version.Id), default);

        Assert.Equal(status, store.Version.ProcessingStatus);
        Assert.Equal(0, provider.Calls);
        Assert.Empty(vectors.BatchSizes);
    }

    [Fact]
    public void Gemini_settings_select_the_new_index_and_collection()
    {
        var settings = new DocumentIndexSettings(
            Options.Create(new DocumentProcessingOptions()),
            Options.Create(new EmbeddingOptions { Dimension = 768 }),
            Options.Create(new QdrantOptions { CollectionName = "nexora_documents_gemini_001", EmbeddingDimension = 768 }));

        Assert.Equal("Gemini", settings.ActiveProvider);
        Assert.Equal("gemini-embedding-001", settings.IndexName);
        Assert.Equal("gemini-embedding-001", settings.EmbeddingModel);
        Assert.Equal("nexora_documents_gemini_001", settings.CollectionName);
        Assert.Equal(768, settings.EmbeddingDimension);
    }

    [Fact]
    public async Task Indexes_five_chunks_per_batch_and_retries_429_without_duplicates()
    {
        var store = new FakeStore(11);
        var provider = new FakeEmbeddingProvider(failFirst: true);
        var scheduler = new FakeScheduler();
        var vectorStore = new FakeVectorStore();
        var handler = CreateHandler(store, provider, scheduler, vectorStore);

        await handler.Handle(new ProcessDocumentVersionCommand(store.Version.Id), default);
        await handler.Handle(new ProcessDocumentVersionCommand(store.Version.Id), default);

        Assert.Equal([5, 5, 1], vectorStore.BatchSizes);
        Assert.Equal(4, provider.Calls);
        Assert.Equal(1, scheduler.RetryCount);
        Assert.Equal(11, store.Records.Count);
        Assert.Equal("READY", store.Version.ProcessingStatus);
        Assert.Null(store.Version.FailureCode);
    }

    [Fact]
    public async Task Resumes_from_indexed_chunks_after_interruption()
    {
        var store = new FakeStore(11, alreadyIndexed: 5);
        var vectorStore = new FakeVectorStore();
        var handler = CreateHandler(store, new FakeEmbeddingProvider(), new FakeScheduler(), vectorStore);

        await handler.Handle(new ProcessDocumentVersionCommand(store.Version.Id), default);

        Assert.Equal([5, 1], vectorStore.BatchSizes);
        Assert.Equal(11, store.Records.Count);
    }

    [Fact]
    public async Task New_chunks_reuse_semantic_vectors_when_indexing()
    {
        var store = new FakeStore(0);
        store.Content["source"] = "first second";
        var provider = new FakeEmbeddingProvider();
        var vectorStore = new FakeVectorStore();
        var handler = CreateHandler(store, provider, new FakeScheduler(), vectorStore,
            chunker: new PresetChunker(), extractor: new PresetExtractor());

        await handler.Handle(new ProcessDocumentVersionCommand(store.Version.Id), default);

        Assert.Equal("READY", store.Version.ProcessingStatus);
        Assert.Equal(2, store.Chunks.Count);
        Assert.Equal(2, store.Contents.Count);
        Assert.Equal(0, provider.Calls);
        Assert.Equal([2], vectorStore.BatchSizes);
        Assert.Equal([1f, 2f], vectorStore.Vectors.Select(vector => vector[0]));
        Assert.Equal(2, store.Content.Keys.Count(key => key.StartsWith("chunks/")));
    }

    [Fact]
    public async Task Gemini_index_becomes_active_only_after_all_documents_are_indexed()
    {
        var store = new FakeStore(6) { OtherDocumentsPending = true };
        store.Version.SetProcessingStatus("READY");
        store.Document.SetStatus("READY");
        var vectorStore = new FakeVectorStore();
        var handler = CreateHandler(store, new FakeEmbeddingProvider(), new FakeScheduler(), vectorStore);

        await handler.Handle(new ProcessDocumentVersionCommand(store.Version.Id), default);
        Assert.Equal("gemini-embedding-001", store.Index!.IndexName);
        Assert.Equal("nexora_documents_gemini_001", store.Index.CollectionName);
        Assert.Equal("BUILDING", store.Index.Status);
        Assert.Equal(["nexora_documents_gemini_001", "nexora_documents_gemini_001"], vectorStore.Collections);

        store.OtherDocumentsPending = false;
        await handler.Handle(new ProcessDocumentVersionCommand(store.Version.Id), default);
        Assert.Equal("ACTIVE", store.Index.Status);
        Assert.Equal("READY", store.Version.ProcessingStatus);
        Assert.Equal(6, store.Records.Count);
    }

    [Fact]
    public async Task Creates_Gemini_index_without_reusing_legacy_model_index()
    {
        var store = new FakeStore(2, legacyIndex: true);
        var legacyId = store.Index!.Id;
        var handler = CreateHandler(store, new FakeEmbeddingProvider(), new FakeScheduler(), new FakeVectorStore());

        await handler.Handle(new ProcessDocumentVersionCommand(store.Version.Id), default);

        Assert.NotEqual(legacyId, store.Index!.Id);
        Assert.Equal("gemini-embedding-001", store.Index.IndexName);
        Assert.Equal("nexora_documents_gemini_001", store.Index.CollectionName);
        Assert.Equal(2, store.Records.Count);
        Assert.Equal("READY", store.Version.ProcessingStatus);
    }

    [Fact]
    public async Task Shared_limiter_rejects_a_twenty_fifth_input_until_window_opens()
    {
        var limiter = new EmbeddingRateLimiter(Options.Create(new DocumentProcessingOptions()), TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<EmbeddingRateLimiter>.Instance);
        await limiter.WaitAsync(24, default);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => limiter.WaitAsync(1, timeout.Token));
    }

    [Fact]
    public async Task Document_batches_wait_for_configured_interval()
    {
        var limiter = new EmbeddingRateLimiter(Options.Create(new DocumentProcessingOptions { BatchIntervalSeconds = 35 }), TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<EmbeddingRateLimiter>.Instance);
        await limiter.WaitForDocumentBatchAsync(default);
        limiter.MarkDocumentBatchCompleted();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => limiter.WaitForDocumentBatchAsync(timeout.Token));
    }

    [Fact]
    public async Task Progress_query_excludes_other_users_documents_and_counts_indexed_chunks()
    {
        var options = new DbContextOptionsBuilder<NexoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCustomizer, TestModelCustomizer>()
            .Options;
        await using var db = new NexoraDbContext(options, clock: new TestClock());
        var org = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var index = RagIndex.Create(org, "gemini-embedding-001", "qdrant", "nexora_documents_gemini_001", "gemini-embedding-001", 2, "Cosine", new string('a', 64), now);
        db.RagIndexes.Add(index);
        for (var documentNo = 0; documentNo < 2; documentNo++)
        {
            var user = documentNo == 0 ? owner : outsider;
            var department = Guid.NewGuid();
            var folder = DocumentFolder.Create(org, department, user, $"folder {documentNo}", now);
            var document = Document.Create(org, department, user, $"document {documentNo}", now);
            var version = DocumentVersion.Create(org, document.Id, 1, $"file {documentNo}.txt", "text/plain", 10,
                "minio", "test", "source", new byte[32], user, now);
            version.SetProcessingStatus("READY", readyAt: now);
            document.SetFolder(folder.Id);
            document.SetCurrentVersion(version.Id);
            document.SetStatus("READY");
            db.DocumentFolders.Add(folder);
            db.Documents.Add(document);
            db.DocumentVersions.Add(version);
            for (var i = 0; i < 3; i++)
            {
                var hash = i.ToString("x").PadLeft(64, '0');
                var chunk = DocumentChunk.Create(org, version.Id, i, hash, now);
                db.DocumentChunks.Add(chunk);
                if (i < 2)
                    db.RagIndexRecords.Add(RagIndexRecord.Create(org, index.Id, chunk.Id, chunk.Id.ToString("D"), hash, now));
            }
        }
        await db.SaveChangesAsync();
        var store = new DocumentProcessingStore(db);

        var progress = await store.GetAccessibleProgressAsync(org, owner, now, "gemini-embedding-001", default);

        Assert.Single(progress);
        Assert.Equal(3, progress[0].TotalChunks);
        Assert.Equal(2, progress[0].IndexedChunks);
        Assert.Equal("ACTIVE", progress[0].IndexStatus);
        Assert.Equal(2, (await store.GetPendingVersionIdsAsync("gemini-embedding-001", default)).Count);

        foreach (var chunk in db.DocumentChunks.Where(chunk => chunk.Ordinal == 2))
            db.RagIndexRecords.Add(RagIndexRecord.Create(org, index.Id, chunk.Id, chunk.Id.ToString("D"), chunk.ContentHash, now));
        index.Status = "BUILDING";
        await db.SaveChangesAsync();
        Assert.Equal(2, (await store.GetPendingVersionIdsAsync("gemini-embedding-001", default)).Count);
    }

    [Fact]
    public async Task Deleting_document_chunks_removes_only_its_chunk_rows_and_index_records()
    {
        var options = new DbContextOptionsBuilder<NexoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ReplaceService<IModelCustomizer, TestModelCustomizer>()
            .Options;
        await using var db = new NexoraDbContext(options, clock: new TestClock());
        var organizationId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var deletedDocumentId = Guid.NewGuid();
        var retainedDocumentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var index = RagIndex.Create(organizationId, "test", "qdrant", "test", "test", 2, "Cosine", new string('a', 64), now);
        db.RagIndexes.Add(index);

        foreach (var documentId in new[] { deletedDocumentId, retainedDocumentId })
        {
            var version = DocumentVersion.Create(organizationId, documentId, 1, "file.txt", "text/plain", 10,
                "minio", "test", "source", new byte[32], ownerId, now);
            var chunk = DocumentChunk.Create(organizationId, version.Id, 0, new string('b', 64), now);
            db.DocumentVersions.Add(version);
            db.DocumentChunks.Add(chunk);
            db.DocumentChunkContents.Add(DocumentChunkContent.Create(chunk.Id, "minio", "test", "chunk", new string('b', 64), now));
            db.RagIndexRecords.Add(RagIndexRecord.Create(organizationId, index.Id, chunk.Id, chunk.Id.ToString("D"), chunk.ContentHash, now));
        }
        await db.SaveChangesAsync();

        await new DocumentProcessingStore(db).DeleteDocumentChunksAsync(organizationId, deletedDocumentId, default);
        await db.SaveChangesAsync();

        var retainedChunk = Assert.Single(db.DocumentChunks);
        Assert.Equal(retainedDocumentId, db.DocumentVersions.Single(version => version.Id == retainedChunk.DocumentVersionId).DocumentId);
        Assert.Equal(retainedChunk.Id, Assert.Single(db.DocumentChunkContents).ChunkId);
        Assert.Equal(retainedChunk.Id, Assert.Single(db.RagIndexRecords).ChunkId);
        Assert.Equal(DeleteBehavior.SetNull, db.Model.FindEntityType(typeof(Citation))!.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(DocumentChunk)).DeleteBehavior);
        Assert.Equal(DeleteBehavior.SetNull, db.Model.FindEntityType(typeof(RetrievalHit))!.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(DocumentChunk)).DeleteBehavior);
    }

    private static ProcessDocumentVersionCommandHandler CreateHandler(FakeStore store, FakeEmbeddingProvider provider,
        FakeScheduler scheduler, FakeVectorStore vectorStore, IDocumentIndexSettings? indexSettings = null,
        IDocumentTextChunker? chunker = null, IDocumentTextExtractor? extractor = null) => new(
        new FakeUnitOfWork(), store, new FakeStorage(store), extractor ?? new UnusedExtractor(), provider, vectorStore,
        new TestClock(), new TestStorageSettings(), indexSettings ?? new TestIndexSettings(), chunker ?? new DocumentTextChunker(),
        new DocumentChunkingProgressStore(), scheduler,
        NullLogger<ProcessDocumentVersionCommandHandler>.Instance);

    private sealed class FakeStore : IDocumentProcessingStore
    {
        public Task<bool> ChangeProcessingStatusAsync(Guid versionId, string from, string to, string documentStatus, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> FinalizeStoppingAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task SaveProcessingChangesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Document Document { get; }
        public DocumentVersion Version { get; }
        public List<DocumentChunk> Chunks { get; } = [];
        public List<DocumentChunkContent> Contents { get; } = [];
        public List<RagIndexRecord> Records { get; } = [];
        public RagIndex? Index { get; private set; }
        public bool OtherDocumentsPending { get; set; }
        public Dictionary<string, string> Content { get; } = [];

        public FakeStore(int chunkCount, int alreadyIndexed = 0, bool legacyIndex = false)
        {
            var org = Guid.NewGuid();
            Document = Document.Create(org, Guid.NewGuid(), Guid.NewGuid(), "Test", DateTimeOffset.UtcNow);
            Version = DocumentVersion.Create(org, Document.Id, 1, "test.txt", "text/plain", 10, "minio", "test",
                "source", new byte[32], Guid.NewGuid(), DateTimeOffset.UtcNow);
            Document.SetCurrentVersion(Version.Id);
            Version.SetProcessingStatus("INDEXING");
            if (alreadyIndexed > 0 || legacyIndex)
                Index = RagIndex.Create(org, legacyIndex ? "default" : "gemini-embedding-001", "qdrant",
                    legacyIndex ? "nexora_documents" : "nexora_documents_gemini_001",
                    legacyIndex ? "text-embedding-004" : "gemini-embedding-001", 2, "Cosine",
                    new string('a', 64), DateTimeOffset.UtcNow);
            for (var i = 0; i < chunkCount; i++)
            {
                var hash = i.ToString("x").PadLeft(64, '0');
                var chunk = DocumentChunk.Create(org, Version.Id, i, hash, DateTimeOffset.UtcNow);
                Chunks.Add(chunk);
                Content[$"chunks/{Version.Id:D}/{i:D}-{hash}.txt"] = $"chunk {i}";
                if (i < alreadyIndexed)
                    Records.Add(RagIndexRecord.Create(org, Index!.Id, chunk.Id, chunk.Id.ToString("D"), hash, DateTimeOffset.UtcNow));
            }
        }

        public Task<DocumentVersion?> GetVersionAsync(Guid versionId, bool asTracking, CancellationToken ct) => Task.FromResult<DocumentVersion?>(Version);
        public Task<Document?> GetDocumentByIdAsync(Guid documentId, bool asTracking, CancellationToken ct) => Task.FromResult<Document?>(Document);
        public Task<List<DocumentChunk>> GetChunksAsync(Guid versionId, bool asTracking, CancellationToken ct) => Task.FromResult(new List<DocumentChunk>(Chunks));
        public Task<RagIndex?> GetIndexAsync(Guid organizationId, string indexName, bool asTracking, CancellationToken ct) =>
            Task.FromResult(Index?.IndexName == indexName ? Index : null);
        public Task<RagIndexRecord?> GetIndexRecordAsync(Guid ragIndexId, Guid chunkId, bool asTracking, CancellationToken ct) =>
            Task.FromResult(Records.SingleOrDefault(record => record.RagIndexId == ragIndexId && record.ChunkId == chunkId));
        public Task<bool> IsIndexCompleteAsync(Guid organizationId, Guid indexId, CancellationToken ct) =>
            Task.FromResult(!OtherDocumentsPending && Chunks.All(chunk => Records.Any(record => record.RagIndexId == indexId && record.ChunkId == chunk.Id)));
        public void InsertIndex(RagIndex index) => Index = index;
        public void InsertIndexRecord(RagIndexRecord record) => Records.Add(record);
        public Task<IReadOnlyList<Guid>> GetPendingVersionIdsAsync(string indexName, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<DocumentProgressRecord>> GetAccessibleProgressAsync(Guid organizationId, Guid userId, DateTimeOffset now, string indexName, CancellationToken ct) => throw new NotImplementedException();
        public Task<Document?> GetDocumentAsync(Guid userId, Guid documentId, bool asTracking, CancellationToken ct) => throw new NotImplementedException();
        public Task<DocumentFolder?> GetFolderAsync(Guid folderId, bool asTracking, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> CanUploadToFolderAsync(DocumentFolder folder, Guid userId, DateTimeOffset utcNow, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<DocumentVersion>> GetDocumentVersionsAsync(Guid organizationId, Guid documentId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<StoredObjectReference>> GetChunkStorageObjectsAsync(Guid organizationId, Guid documentId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<VectorPointReference>> GetDocumentVectorPointsAsync(Guid organizationId, Guid documentId, CancellationToken ct) => throw new NotImplementedException();
        public Task DeleteDocumentChunksAsync(Guid organizationId, Guid documentId, CancellationToken ct) => throw new NotImplementedException();
        public Task<int> GetNextVersionNoAsync(Guid documentId, CancellationToken ct) => throw new NotImplementedException();
        public Task<DocumentChunkContent?> GetChunkContentAsync(Guid chunkId, bool asTracking, CancellationToken ct) => throw new NotImplementedException();
        public void InsertDocument(Document document) => throw new NotImplementedException();
        public void InsertVersion(DocumentVersion version) => throw new NotImplementedException();
        public void InsertChunk(DocumentChunk chunk) => Chunks.Add(chunk);
        public void InsertChunkContent(DocumentChunkContent content) => Contents.Add(content);
    }

    private sealed class FakeStorage(FakeStore store) : IObjectStorage
    {
        public Task<Stream> GetAsync(string bucket, string objectKey, CancellationToken ct) =>
            Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(store.Content[objectKey])));
        public async Task PutAsync(string bucket, string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken ct)
        {
            using var reader = new StreamReader(content, leaveOpen: true);
            store.Content[objectKey] = await reader.ReadToEndAsync(ct);
        }
        public Task DeleteAsync(string bucket, string objectKey, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class UnusedExtractor : IDocumentTextExtractor
    {
        public Task<ExtractedDocumentText> ExtractAsync(string fileName, Stream content, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class PresetExtractor : IDocumentTextExtractor
    {
        public Task<ExtractedDocumentText> ExtractAsync(string fileName, Stream content, CancellationToken ct) =>
            Task.FromResult(new ExtractedDocumentText("first second", "test", "1"));
    }

    private sealed class PresetChunker : IDocumentTextChunker
    {
        public Task<IReadOnlyList<TextChunk>> ChunkAsync(string text, int chunkSize, int overlap,
            CancellationToken ct, Action<DocumentChunkingProgress>? reportProgress = null) =>
            Task.FromResult<IReadOnlyList<TextChunk>>([
                new TextChunk(0, "first", 0, 5, [1f, 0f]),
                new TextChunk(1, "second", 6, 12, [2f, 0f])
            ]);
    }

    private sealed class FakeEmbeddingProvider(bool failFirst = false) : IEmbeddingProvider
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken ct)
        {
            Calls++;
            if (failFirst && Calls == 1) throw new HttpRequestException("rate limited", null, HttpStatusCode.TooManyRequests);
            return Task.FromResult<IReadOnlyList<float[]>>(inputs.Select(_ => new float[] { 1, 2 }).ToArray());
        }
    }

    private sealed class FakeScheduler : IEmbeddingBatchScheduler
    {
        public int RetryCount { get; private set; }
        public Task WaitForDocumentBatchAsync(CancellationToken ct) => Task.CompletedTask;
        public void MarkDocumentBatchCompleted() { }
        public Task WaitAfterRateLimitAsync(CancellationToken ct) { RetryCount++; return Task.CompletedTask; }
    }

    private sealed class FakeVectorStore : IVectorStore
    {
        public List<int> BatchSizes { get; } = [];
        public List<string> Collections { get; } = [];
        public List<float[]> Vectors { get; } = [];
        public Task EnsureCollectionAsync(string collectionName, int dimension, string distance, CancellationToken ct) => Task.CompletedTask;
        public Task UpsertAsync(string collectionName, IReadOnlyCollection<VectorPoint> points, CancellationToken ct)
        {
            BatchSizes.Add(points.Count);
            Collections.Add(collectionName);
            Vectors.AddRange(points.Select(point => point.Vector));
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collectionName, float[] vector, IReadOnlyCollection<Guid> allowedDocumentIds, int limit, CancellationToken ct) => throw new NotImplementedException();
        public Task DeleteAsync(string collectionName, IReadOnlyCollection<string> pointIds, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class TestClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
    private sealed class TestStorageSettings : IDocumentStorageSettings { public string Bucket => "test"; }
    private sealed class TestIndexSettings : IDocumentIndexSettings
    {
        public string ActiveProvider => "Gemini";
        public string IndexName => "gemini-embedding-001";
        public int BatchSize => 5;
        public int ChunkSizeCharacters => 1200;
        public int ChunkOverlapCharacters => 200;
        public int EmbeddingDimension => 2;
        public int VectorDimension => 2;
        public string CollectionName => "nexora_documents_gemini_001";
        public string EmbeddingModel => "gemini-embedding-001";
        public string Distance => "Cosine";
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public IUserRepository UserRepository => throw new NotImplementedException();
        public IRoleRepository RoleRepository => throw new NotImplementedException();
        public IOrganizationRepository OrganizationRepository => throw new NotImplementedException();
        public IUserProfileRepository UserProfileRepository => throw new NotImplementedException();
        public IOrganizationMembershipRepository OrganizationMembershipRepository => throw new NotImplementedException();
        public IOrganizationRoleAssignmentRepository OrganizationRoleAssignmentRepository => throw new NotImplementedException();
        public IDepartmentRepository DepartmentRepository => throw new NotImplementedException();
        public IDepartmentMembershipRepository DepartmentMembershipRepository => throw new NotImplementedException();
        public IApiFunctionRepository ApiFunctionRepository => throw new NotImplementedException();
        public IRoleApiFunctionRepository RoleApiFunctionRepository => throw new NotImplementedException();
        public IAccessTokenRepository AccessTokenRepository => throw new NotImplementedException();
        public IActivityLogRepository ActivityLogRepository => throw new NotImplementedException();
        public IDocumentFolderRepository DocumentFolderRepository => throw new NotImplementedException();
        public IDocumentFolderAccessGrantRepository DocumentFolderAccessGrantRepository => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct) => operation(ct);
    }

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
