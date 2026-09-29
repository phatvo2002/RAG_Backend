using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Infrastructure.Options;

namespace Nexora.Infrastructure.Integrations.Embeddings;

internal sealed class GeminiEmbeddingProvider(HttpClient httpClient, IOptions<EmbeddingOptions> options) : IEmbeddingProvider
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

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
        request.Headers.Add("x-goog-api-key", settings.ApiKey);
        request.Content = JsonContent.Create(new BatchEmbeddingRequest(inputs
            .Select(input => new EmbeddingRequest(
                $"models/{settings.Model}",
                new EmbeddingContent([new EmbeddingPart(input)]),
                new EmbeddingConfig(settings.Dimension)))
            .ToArray()));
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
            throw new InvalidOperationException("Embedding provider trả về số lượng hoặc kích thước vector không hợp lệ.");
        }

        return vectors;
    }

    private sealed record BatchEmbeddingRequest([property: JsonPropertyName("requests")] EmbeddingRequest[] Requests);
    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("content")] EmbeddingContent Content,
        [property: JsonPropertyName("embedContentConfig")] EmbeddingConfig Config);
    private sealed record EmbeddingConfig([property: JsonPropertyName("outputDimensionality")] int OutputDimensionality);
    private sealed record EmbeddingContent([property: JsonPropertyName("parts")] EmbeddingPart[] Parts);
    private sealed record EmbeddingPart([property: JsonPropertyName("text")] string Text);
    private sealed record EmbeddingResponse([property: JsonPropertyName("embeddings")] EmbeddingItem[] Embeddings);
    private sealed record EmbeddingItem([property: JsonPropertyName("values")] float[] Values);
}
