using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Integrations.Embeddings;

internal sealed class GeminiEmbeddingProvider(HttpClient httpClient, IOptions<EmbeddingOptions> options, IEmbeddingRateLimiter limiter, Microsoft.Extensions.Logging.ILogger<GeminiEmbeddingProvider> logger) : IEmbeddingProvider
{
    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Embedding:Endpoint và Embedding:ApiKey phải được cấu hình.");
        }

        if (inputs.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        await limiter.WaitAsync(inputs.Count, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
        request.Headers.Add("x-goog-api-key", settings.ApiKey);
        request.Content = JsonContent.Create(new BatchEmbeddingRequest(inputs
            .Select(input => new EmbeddingRequest(
                $"models/{EmbeddingOptions.ModelName}",
                new EmbeddingContent([new EmbeddingPart(input)]),
                settings.Dimension))
            .ToArray()));
        var started = System.Diagnostics.Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        logger.LogInformation("Gemini embedding request: {InputCount} inputs, {ElapsedMs} ms, HTTP {StatusCode}.",
            inputs.Count, started.ElapsedMilliseconds, (int)response.StatusCode);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Gemini embedding API returned {(int)response.StatusCode} ({response.StatusCode}): {errorBody}",
                null,
                response.StatusCode);
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Embedding provider trả về response rỗng.");
        var vectors = payload.Embeddings.Select(item => item.Values).ToArray();
        if (vectors.Length != inputs.Count || vectors.Any(vector => vector.Length != settings.Dimension))
        {
            var dimensions = string.Join(", ", vectors.Select(vector => vector.Length));
            throw new InvalidOperationException($"Embedding provider trả về {vectors.Length} vector (cần {inputs.Count}), số chiều [{dimensions}] (cần {settings.Dimension}).");
        }

        return vectors;
    }

    private sealed record BatchEmbeddingRequest([property: JsonPropertyName("requests")] EmbeddingRequest[] Requests);
    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("content")] EmbeddingContent Content,
        [property: JsonPropertyName("outputDimensionality")] int OutputDimensionality);
    private sealed record EmbeddingContent([property: JsonPropertyName("parts")] EmbeddingPart[] Parts);
    private sealed record EmbeddingPart([property: JsonPropertyName("text")] string Text);
    private sealed record EmbeddingResponse([property: JsonPropertyName("embeddings")] EmbeddingItem[] Embeddings);
    private sealed record EmbeddingItem([property: JsonPropertyName("values")] float[] Values);
}
