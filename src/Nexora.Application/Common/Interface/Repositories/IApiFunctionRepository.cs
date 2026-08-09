using Nexora.Domain.Authorization;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IApiFunctionRepository : IBaseRepository<ApiFunction>
{
    Task<ApiFunction?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task AddAsync(
        ApiFunction function,
        CancellationToken cancellationToken);
}
