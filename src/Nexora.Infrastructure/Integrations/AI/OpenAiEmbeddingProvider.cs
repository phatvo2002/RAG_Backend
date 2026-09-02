using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Infrastructure.Options;

namespace Nexora.Infrastructure.Rag;

internal sealed class OpenAiEmbeddingProvider(HttpClient httpClient, IOptions<EmbeddingOptions> options) : IEmbeddingProvider
{
    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Endpoint) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Embedding:Endpoint và Embedding:ApiKey phải được cấu hình.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = JsonContent.Create(new EmbeddingRequest(settings.Model, inputs.ToArray()));
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Embedding provider trả về response rỗng.");
        var vectors = payload.Data.OrderBy(item => item.Index).Select(item => item.Embedding).ToArray();
        if (vectors.Length != inputs.Count || vectors.Any(vector => vector.Length != settings.Dimension))
        {
            throw new InvalidOperationException("Embedding provider trả về số lượng hoặc kích thước vector không hợp lệ.");
        }

        return vectors;
    }

    private sealed record EmbeddingRequest(string Model, string[] Input);
    private sealed record EmbeddingResponse([property: JsonPropertyName("data")] EmbeddingItem[] Data);
    private sealed record EmbeddingItem([property: JsonPropertyName("embedding")] float[] Embedding, [property: JsonPropertyName("index")] int Index);
}
