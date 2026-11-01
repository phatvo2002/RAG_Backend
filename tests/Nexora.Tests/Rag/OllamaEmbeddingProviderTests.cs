using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Infrastructure.Services.Integrations.Embeddings;

namespace Nexora.Tests.Rag;

public sealed class OllamaEmbeddingProviderTests
{
    [Fact]
    public async Task Batch_uses_local_model_and_expected_dimensions_without_key()
    {
        var handler = new StubHandler(2, 1024);
        var provider = CreateProvider(handler);

        var vectors = await provider.EmbedAsync(["first", "second"], default);

        Assert.Equal(2, vectors.Count);
        Assert.All(vectors, vector => Assert.Equal(1024, vector.Length));
        Assert.Equal("http://ollama:11434/api/embed", handler.Url);
        Assert.Null(handler.ApiKey);
        using var json = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("qwen3-embedding:0.6b", json.RootElement.GetProperty("model").GetString());
        Assert.Equal(1024, json.RootElement.GetProperty("dimensions").GetInt32());
        Assert.False(json.RootElement.GetProperty("truncate").GetBoolean());
        Assert.Equal(2, json.RootElement.GetProperty("input").GetArrayLength());
    }

    [Fact]
    public async Task Invalid_dimension_rejects_response()
    {
        var provider = CreateProvider(new StubHandler(1, 768));
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.EmbedAsync(["query"], default));
    }

    [Fact]
    public async Task Missing_embedding_model_fails_without_gemini_fallback()
    {
        var provider = CreateProvider(new StubHandler(0, 0, HttpStatusCode.NotFound));
        await Assert.ThrowsAsync<HttpRequestException>(() => provider.EmbedAsync(["query"], default));
    }

    [Fact]
    public async Task Query_embedding_receives_chat_priority()
    {
        var scheduler = new RecordingScheduler();
        var provider = CreateProvider(new StubHandler(1, 1024), scheduler);
        await provider.EmbedQueryAsync(["query"], default);
        Assert.Equal(1, scheduler.ChatCalls);
        Assert.Equal(0, scheduler.EmbeddingCalls);
    }

    private static OllamaEmbeddingProvider CreateProvider(StubHandler handler, RecordingScheduler? scheduler = null) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://ollama:11434/") },
        Options.Create(new EmbeddingOptions()), scheduler ?? new RecordingScheduler());

    private sealed class RecordingScheduler : ILocalInferenceScheduler
    {
        public int ChatCalls { get; private set; }
        public int EmbeddingCalls { get; private set; }
        public Task<IDisposable> AcquireChatAsync(CancellationToken ct)
        {
            ChatCalls++;
            return Task.FromResult<IDisposable>(new Lease());
        }
        public Task<IDisposable> AcquireEmbeddingAsync(CancellationToken ct)
        {
            EmbeddingCalls++;
            return Task.FromResult<IDisposable>(new Lease());
        }
        private sealed class Lease : IDisposable { public void Dispose() { } }
    }

    private sealed class StubHandler(int vectorCount, int dimension, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }
        public string? Url { get; private set; }
        public string? ApiKey { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri?.ToString();
            ApiKey = request.Headers.TryGetValues("x-goog-api-key", out var values) ? values.Single() : null;
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = JsonContent.Create(new { embeddings = Enumerable.Range(0, vectorCount)
                    .Select(_ => Enumerable.Repeat(0.1f, dimension).ToArray()).ToArray() })
            };
        }
    }
}
