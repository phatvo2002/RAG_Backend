using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Integrations.Embeddings;

internal sealed class OllamaEmbeddingProvider(HttpClient client, IOptions<EmbeddingOptions> options,
    ILocalInferenceScheduler scheduler) : IEmbeddingProvider
{
    public Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken) =>
        GenerateAsync(inputs, false, cancellationToken);

    public Task<IReadOnlyList<float[]>> EmbedQueryAsync(IReadOnlyCollection<string> inputs, CancellationToken cancellationToken) =>
        GenerateAsync(inputs, true, cancellationToken);

    private async Task<IReadOnlyList<float[]>> GenerateAsync(IReadOnlyCollection<string> inputs, bool chatPriority,
        CancellationToken cancellationToken)
    {
        if (inputs.Count == 0) return [];
        using var lease = chatPriority ? await scheduler.AcquireChatAsync(cancellationToken)
            : await scheduler.AcquireEmbeddingAsync(cancellationToken);
        using var response = await client.PostAsJsonAsync("api/embed", new
        {
            model = options.Value.Model,
            input = inputs,
            dimensions = options.Value.Dimension,
            truncate = false
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty embedding response.");
        var vectors = payload.Embeddings;
        if (vectors is null || vectors.Length != inputs.Count || vectors.Any(vector => vector is null || vector.Length != options.Value.Dimension))
            throw new InvalidOperationException("Ollama returned an unexpected embedding count or dimension.");
        return vectors;
    }

    private sealed record EmbeddingResponse(float[][] Embeddings);
}
