namespace Nexora.Application.Common.Abstractions.Rag;

public interface ILocalInferenceScheduler
{
    Task<IDisposable> AcquireChatAsync(CancellationToken cancellationToken);
    Task<IDisposable> AcquireEmbeddingAsync(CancellationToken cancellationToken);
}
