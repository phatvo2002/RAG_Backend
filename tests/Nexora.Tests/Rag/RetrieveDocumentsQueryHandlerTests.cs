using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Rag.Retrieval.Query;
using Nexora.Domain.Documents;
using Nexora.Domain.Rag;

namespace Nexora.Tests.Rag;

public sealed class RetrieveDocumentsQueryHandlerTests
{
    [Fact]
    public async Task Handle_DeniesRelevantInaccessibleDepartmentBeforeLoadingAnyChunkContent()
    {
        var documentId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var vectorStore = new FakeVectorStore(new VectorSearchResult(documentId, Guid.NewGuid(), 0.91f));
        var storage = new FakeObjectStorage();
        var retrievalStore = new FakeRetrievalStore(documentId);
        var handler = new RetrieveDocumentsQueryHandler(
            new FakeOrganizationProvider(organizationId),
            new FakeUserContext(),
            new FakeProcessingStore(),
            retrievalStore,
            new FakeEmbeddingProvider(),
            vectorStore,
            storage,
            new FakeClock(),
            new FakeScopeOptions(0.70f),
            new FakeIndexSettings());

        var response = await handler.Handle(new RetrieveDocumentsQuery("How does HR set compensation?"), CancellationToken.None);

        Assert.True(response.DepartmentAccessDenied);
        Assert.Empty(response.Results);
        Assert.Empty(response.AugmentedContext);
        Assert.Equal(1, vectorStore.SearchCount);
        Assert.Equal(0, storage.ReadCount);
        Assert.Equal(0, retrievalStore.SourceLookupCount);
    }

    [Fact]
    public async Task Ignores_legacy_index_when_Gemini_index_is_missing()
    {
        var documentId = Guid.NewGuid();
        var retrievalStore = new FakeRetrievalStore(documentId);
        var provider = new FakeEmbeddingProvider();
        var handler = new RetrieveDocumentsQueryHandler(
            new FakeOrganizationProvider(Guid.NewGuid()), new FakeUserContext(),
            new FakeProcessingStore(missingGeminiIndex: true), retrievalStore, provider,
            new FakeVectorStore(new VectorSearchResult(documentId, Guid.NewGuid(), 0.91f)),
            new FakeObjectStorage(), new FakeClock(), new FakeScopeOptions(0.70f),
            new FakeIndexSettings());

        var response = await handler.Handle(new RetrieveDocumentsQuery("question"), default);

        Assert.Empty(response.Results);
        Assert.Equal(0, provider.Calls);
        Assert.Null(retrievalStore.LastIndexName);
    }

    [Fact]
    public async Task Uses_only_Gemini_index_after_reindex_is_complete()
    {
        var documentId = Guid.NewGuid();
        var retrievalStore = new FakeRetrievalStore(documentId);
        var provider = new FakeEmbeddingProvider();
        var handler = new RetrieveDocumentsQueryHandler(
            new FakeOrganizationProvider(Guid.NewGuid()), new FakeUserContext(),
            new FakeProcessingStore(), retrievalStore, provider,
            new FakeVectorStore(new VectorSearchResult(documentId, Guid.NewGuid(), 0.91f)),
            new FakeObjectStorage(), new FakeClock(), new FakeScopeOptions(0.70f),
            new FakeIndexSettings());

        await handler.Handle(new RetrieveDocumentsQuery("question"), default);

        Assert.Equal(1, provider.Calls);
        Assert.Equal("gemini-embedding-001", retrievalStore.LastIndexName);
    }

    [Fact]
    public async Task Sends_full_authorized_chunk_to_context_but_short_excerpt_to_client()
    {
        var documentId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var text = new string('A', 600) + "ANSWER_AT_END";
        var storage = new FakeObjectStorage(text);
        var handler = new RetrieveDocumentsQueryHandler(
            new FakeOrganizationProvider(Guid.NewGuid()), new FakeUserContext(), new FakeProcessingStore(),
            new FakeRetrievalStore(documentId, chunkId), new FakeEmbeddingProvider(),
            new FakeVectorStore(new VectorSearchResult(documentId, chunkId, 0.91f)),
            storage, new FakeClock(), new FakeScopeOptions(0.70f), new FakeIndexSettings());

        var response = await handler.Handle(new RetrieveDocumentsQuery("question"), default);

        Assert.Contains("ANSWER_AT_END", response.AugmentedContext);
        Assert.DoesNotContain("ANSWER_AT_END", Assert.Single(response.Results).Excerpt);
        Assert.Equal(601, response.Results[0].Excerpt.Length);
        Assert.Equal("rag-context-v4", response.PromptVersion);
        Assert.Equal(1, storage.ReadCount);
    }

    private sealed class FakeOrganizationProvider(Guid organizationId) : IDefaultOrganizationProvider
    {
        public Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken) => Task.FromResult<Guid?>(organizationId);
    }

    private sealed class FakeUserContext : IUserContext
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public bool IsAuthenticated => true;
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class FakeScopeOptions(float departmentMatchThreshold) : IDepartmentScopeOptions
    {
        public float DepartmentMatchThreshold { get; } = departmentMatchThreshold;
    }

    private sealed class FakeIndexSettings : IDocumentIndexSettings
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

    private sealed class FakeEmbeddingProvider : IEmbeddingProvider
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<float[]>>([new float[] { 0.1f, 0.2f }]);
        }
    }

    private sealed class FakeVectorStore(VectorSearchResult companyMatch) : IVectorStore
    {
        public int SearchCount { get; private set; }

        public Task EnsureCollectionAsync(string collectionName, int dimension, string distance, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task UpsertAsync(string collectionName, IReadOnlyCollection<VectorPoint> points, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collectionName, float[] vector,
            IReadOnlyCollection<Guid> allowedDocumentIds, int limit, CancellationToken cancellationToken)
        {
            SearchCount++;
            Assert.Contains(companyMatch.DocumentId, allowedDocumentIds);
            Assert.True(limit is 1 or 5);
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>([companyMatch]);
        }

        public Task DeleteAsync(string collectionName, IReadOnlyCollection<string> pointIds, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class FakeObjectStorage(string? content = null) : IObjectStorage
    {
        public int ReadCount { get; private set; }

        public Task PutAsync(string bucket, string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Stream> GetAsync(string bucket, string objectKey, CancellationToken cancellationToken)
        {
            ReadCount++;
            if (content is null) throw new InvalidOperationException("Inaccessible department content must not be loaded.");
            return Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));
        }

        public Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class FakeRetrievalStore(Guid inaccessibleDepartmentDocumentId, Guid? accessibleChunkId = null) : IDocumentRetrievalStore
    {
        public int SourceLookupCount { get; private set; }
        public string? LastIndexName { get; private set; }

        public Task<string?> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("Nexora");

        public Task<IReadOnlyList<IndexedDocumentReference>> GetIndexedDocumentsAsync(Guid organizationId, string indexName, CancellationToken cancellationToken)
        {
            LastIndexName = indexName;
            return Task.FromResult<IReadOnlyList<IndexedDocumentReference>>([new IndexedDocumentReference(inaccessibleDepartmentDocumentId, true)]);
        }

        public Task<IReadOnlyList<Guid>> GetAccessibleIndexedDocumentIdsAsync(Guid organizationId, Guid userId, DateTimeOffset now, string indexName, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>(accessibleChunkId.HasValue ? [inaccessibleDepartmentDocumentId] : []);

        public Task<IReadOnlyList<RetrievalChunkSource>> GetAccessibleChunkSourcesAsync(Guid organizationId, Guid userId,
            DateTimeOffset now, IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken)
        {
            SourceLookupCount++;
            if (!accessibleChunkId.HasValue) throw new InvalidOperationException("No authorized source lookup is expected after an access denial.");
            return Task.FromResult<IReadOnlyList<RetrievalChunkSource>>([
                new RetrievalChunkSource(inaccessibleDepartmentDocumentId, accessibleChunkId.Value, "source.txt", null,
                    null, 0, 613, "test", "chunk")
            ]);
        }
    }

    private sealed class FakeProcessingStore(bool missingGeminiIndex = false) : IDocumentProcessingStore
    {
        public Task<IReadOnlyList<Guid>> GetPendingVersionIdsAsync(string indexName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> IsIndexCompleteAsync(Guid organizationId, Guid indexId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<DocumentProgressRecord>> GetAccessibleProgressAsync(Guid organizationId, Guid userId, DateTimeOffset now, string indexName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<RagIndex?> GetIndexAsync(Guid organizationId, string indexName, bool asTracking, CancellationToken cancellationToken)
        {
            if (missingGeminiIndex) return Task.FromResult<RagIndex?>(null);
            var index = RagIndex.Create(organizationId, indexName, "qdrant", "nexora_documents_gemini_001", "gemini-embedding-001", 2, "Cosine", new string('a', 64), DateTimeOffset.UtcNow);
            return Task.FromResult<RagIndex?>(index);
        }

        public Task<Document?> GetDocumentAsync(Guid userId, Guid documentId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Document?> GetDocumentByIdAsync(Guid documentId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<DocumentFolder?> GetFolderAsync(Guid folderId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> CanUploadToFolderAsync(DocumentFolder folder, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<DocumentVersion>> GetDocumentVersionsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<StoredObjectReference>> GetChunkStorageObjectsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<VectorPointReference>> GetDocumentVectorPointsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task DeleteDocumentChunksAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<DocumentVersion?> GetVersionAsync(Guid versionId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<int> GetNextVersionNoAsync(Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<List<DocumentChunk>> GetChunksAsync(Guid versionId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<DocumentChunkContent?> GetChunkContentAsync(Guid chunkId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<RagIndexRecord?> GetIndexRecordAsync(Guid ragIndexId, Guid chunkId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public void InsertDocument(Document document) => throw new NotImplementedException();
        public void InsertVersion(DocumentVersion version) => throw new NotImplementedException();
        public void InsertChunk(DocumentChunk chunk) => throw new NotImplementedException();
        public void InsertChunkContent(DocumentChunkContent content) => throw new NotImplementedException();
        public void InsertIndex(RagIndex index) => throw new NotImplementedException();
        public void InsertIndexRecord(RagIndexRecord record) => throw new NotImplementedException();
    }
}
