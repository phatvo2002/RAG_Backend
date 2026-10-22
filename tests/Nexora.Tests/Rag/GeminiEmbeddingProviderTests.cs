using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Infrastructure.Services.Integrations.Embeddings;

namespace Nexora.Tests.Rag;

public sealed class GeminiEmbeddingProviderTests
{
    [Fact]
    public async Task Batch_request_sets_output_dimension_for_each_input()
    {
        var handler = new StubHandler(2, 768);
        var provider = CreateProvider(handler);

        var vectors = await provider.EmbedAsync(["first", "second"], default);

        Assert.Equal(2, vectors.Count);
        Assert.All(vectors, vector => Assert.Equal(768, vector.Length));
        using var request = JsonDocument.Parse(handler.RequestBody!);
        var items = request.RootElement.GetProperty("requests").EnumerateArray().ToArray();
        Assert.Equal(2, items.Length);
        Assert.All(items, item =>
        {
            Assert.Equal("models/gemini-embedding-001", item.GetProperty("model").GetString());
            Assert.Equal(768, item.GetProperty("outputDimensionality").GetInt32());
            Assert.False(item.TryGetProperty("embedContentConfig", out _));
        });
    }

    [Fact]
    public async Task Unexpected_dimension_reports_received_size()
    {
        var provider = CreateProvider(new StubHandler(1, 3072));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.EmbedAsync(["query"], default));

        Assert.Contains("3072", error.Message);
        Assert.Contains("768", error.Message);
    }

    private static GeminiEmbeddingProvider CreateProvider(StubHandler handler) => new(
        new HttpClient(handler),
        Options.Create(new EmbeddingOptions
        {
            Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:batchEmbedContents",
            ApiKey = "test-key",
            Dimension = 768
        }), new NoWaitLimiter(), Microsoft.Extensions.Logging.Abstractions.NullLogger<GeminiEmbeddingProvider>.Instance);

    private sealed class NoWaitLimiter : IEmbeddingRateLimiter
    {
        public Task WaitAsync(int inputCount, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubHandler(int vectorCount, int dimension) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    embeddings = Enumerable.Range(0, vectorCount)
                        .Select(_ => new { values = Enumerable.Repeat(0.1f, dimension).ToArray() })
                        .ToArray()
                })
            };
        }
    }
}
