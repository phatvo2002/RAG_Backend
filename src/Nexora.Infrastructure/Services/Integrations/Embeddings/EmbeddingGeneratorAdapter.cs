using Microsoft.Extensions.AI;
using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Integrations.Embeddings;

internal sealed class EmbeddingGeneratorAdapter(IEmbeddingProvider provider) : IEmbeddingGenerator<string, Embedding<float>>
{
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var inputs = values.ToArray();
        var vectors = await provider.EmbedAsync(inputs, cancellationToken);
        return new GeneratedEmbeddings<Embedding<float>>(vectors.Select(vector => new Embedding<float>(vector)));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}
