using System.Net;
using System.Text.Json;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Features.V1.Rag.Retrieval.Query;
using Nexora.Application.Features.V1.Rag.Retrieval.Validation;
using Nexora.Infrastructure.Services.Integrations.VectorStores;

namespace Nexora.Tests.Rag;

public sealed class QdrantVectorSearchTests
{
    [Fact]
    public async Task SearchAsync_FiltersToAllowedDocumentsAndMapsPoints()
    {
        var allowedDocumentId = Guid.NewGuid();
        var otherDocumentId = Guid.NewGuid();
        var allowedChunkId = Guid.NewGuid();
        var otherChunkId = Guid.NewGuid();
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            var responseBody = $$"""
                {
                  "result": {
                    "points": [
                      { "id": "{{allowedChunkId:D}}", "score": 0.91, "payload": { "documentId": "{{allowedDocumentId:D}}", "chunkId": "{{allowedChunkId:D}}" } },
                      { "id": "{{otherChunkId:D}}", "score": 0.88, "payload": { "documentId": "{{otherDocumentId:D}}", "chunkId": "{{otherChunkId:D}}" } }
                    ]
                  }
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody) };
        });
        var store = new QdrantVectorStore(new HttpClient(handler) { BaseAddress = new Uri("http://qdrant/") });

        var results = await store.SearchAsync("nexora_documents", [0.1f, 0.2f], [allowedDocumentId], 5, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(allowedDocumentId, result.DocumentId);
        Assert.Equal(allowedChunkId, result.ChunkId);
        Assert.Equal(0.91f, result.Score);
        using var request = JsonDocument.Parse(requestBody!);
        var json = request.RootElement;
        Assert.Equal(5, json.GetProperty("limit").GetInt32());
        Assert.Equal("documentId", json.GetProperty("filter").GetProperty("must")[0].GetProperty("key").GetString());
        Assert.Equal(allowedDocumentId.ToString("D"), json.GetProperty("filter").GetProperty("must")[0].GetProperty("match").GetProperty("any")[0].GetString());
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyWithoutCallingQdrantWhenNoDocumentsAreAllowed()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new InvalidOperationException("Qdrant should not be called."));
        var store = new QdrantVectorStore(new HttpClient(handler) { BaseAddress = new Uri("http://qdrant/") });

        var results = await store.SearchAsync("nexora_documents", [0.1f], [], 5, CancellationToken.None);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RetrievalQueryValidator_RejectsEmptyQuestion(string question)
    {
        var result = new RetrieveDocumentsQueryValidator().Validate(new RetrieveDocumentsQuery(question));

        Assert.False(result.IsValid);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            sendAsync(request, cancellationToken);
    }
}
