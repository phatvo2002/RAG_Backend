namespace Nexora.Application.Common.Abstractions.Rag;

public interface IEmbeddingRateLimiter
{
    Task WaitAsync(int inputCount, CancellationToken cancellationToken);
}
