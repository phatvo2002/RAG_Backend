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
            new FakeScopeOptions(0.70f));

        var response = await handler.Handle(new RetrieveDocumentsQuery("How does HR set compensation?"), CancellationToken.None);

        Assert.True(response.DepartmentAccessDenied);
        Assert.Empty(response.Results);
        Assert.Empty(response.AugmentedContext);
        Assert.Equal(1, vectorStore.SearchCount);
        Assert.Equal(0, storage.ReadCount);
        Assert.Equal(0, retrievalStore.SourceLookupCount);
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

    private sealed class FakeEmbeddingProvider : IEmbeddingProvider
    {
        public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<float[]>>([new float[] { 0.1f, 0.2f }]);
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
            Assert.Equal(1, limit);
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>([companyMatch]);
        }

        public Task DeleteAsync(string collectionName, IReadOnlyCollection<string> pointIds, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class FakeObjectStorage : IObjectStorage
    {
        public int ReadCount { get; private set; }

        public Task PutAsync(string bucket, string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Stream> GetAsync(string bucket, string objectKey, CancellationToken cancellationToken)
        {
            ReadCount++;
            throw new InvalidOperationException("Inaccessible department content must not be loaded.");
        }

        public Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class FakeRetrievalStore(Guid inaccessibleDepartmentDocumentId) : IDocumentRetrievalStore
    {
        public int SourceLookupCount { get; private set; }

        public Task<string?> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("Nexora");

        public Task<IReadOnlyList<IndexedDocumentReference>> GetIndexedDocumentsAsync(Guid organizationId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IndexedDocumentReference>>([new IndexedDocumentReference(inaccessibleDepartmentDocumentId, true)]);

        public Task<IReadOnlyList<Guid>> GetAccessibleIndexedDocumentIdsAsync(Guid organizationId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<RetrievalChunkSource>> GetAccessibleChunkSourcesAsync(Guid organizationId, Guid userId,
            DateTimeOffset now, IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken)
        {
            SourceLookupCount++;
            throw new InvalidOperationException("No authorized source lookup is expected after an access denial.");
        }
    }

    private sealed class FakeProcessingStore : IDocumentProcessingStore
    {
        public Task<RagIndex?> GetIndexAsync(Guid organizationId, string indexName, bool asTracking, CancellationToken cancellationToken) =>
            Task.FromResult<RagIndex?>(RagIndex.Create(organizationId, indexName, "qdrant", "nexora_documents", "embedding", 2, "Cosine", new string('a', 64), DateTimeOffset.UtcNow));

        public Task<Document?> GetDocumentAsync(Guid userId, Guid documentId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Document?> GetDocumentByIdAsync(Guid documentId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<DocumentFolder?> GetFolderAsync(Guid folderId, bool asTracking, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<bool> CanUploadToFolderAsync(DocumentFolder folder, Guid userId, DateTimeOffset utcNow, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<DocumentVersion>> GetDocumentVersionsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<StoredObjectReference>> GetChunkStorageObjectsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<IReadOnlyList<VectorPointReference>> GetDocumentVectorPointsAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task MarkDocumentVectorRecordsDeletedAsync(Guid organizationId, Guid documentId, CancellationToken cancellationToken) => throw new NotImplementedException();
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
