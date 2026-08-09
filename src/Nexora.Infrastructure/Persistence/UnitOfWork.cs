using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Results;

namespace Nexora.Infrastructure.Persistence;

internal sealed class UnitOfWork(NexoraDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await operation(cancellationToken);

            if (response is IApplicationResult { IsSuccess: false })
            {
                await transaction.RollbackAsync(cancellationToken);
                return response;
            }

            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
