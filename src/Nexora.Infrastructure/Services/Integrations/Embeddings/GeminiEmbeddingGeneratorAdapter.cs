using System.Net;
using Microsoft.Extensions.AI;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Integrations.Embeddings;

internal sealed class GeminiEmbeddingGeneratorAdapter(
    IEmbeddingProvider provider,
    IEmbeddingBatchScheduler scheduler) : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var inputs = values.ToArray();
        if (inputs.Length == 0) return new GeneratedEmbeddings<Embedding<float>>([]);

        await gate.WaitAsync(cancellationToken);
        try
        {
            while (true)
            {
                try
                {
                    var vectors = await provider.EmbedAsync(inputs, cancellationToken);
                    return new GeneratedEmbeddings<Embedding<float>>(
                        vectors.Select(vector => new Embedding<float>(vector)));
                }
                catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    await scheduler.WaitAfterRateLimitAsync(cancellationToken);
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() => gate.Dispose();
}
