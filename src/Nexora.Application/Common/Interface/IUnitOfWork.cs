namespace Nexora.Application.Common.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken);
}
