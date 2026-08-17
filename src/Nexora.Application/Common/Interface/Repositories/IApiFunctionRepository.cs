using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IApiFunctionRepository : IBaseRepository<ApiFunction>
{
    Task<ApiFunction?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<List<ApiFunction>> GetActiveByIdsAsync(IReadOnlyCollection<Guid> functionIds, CancellationToken cancellationToken);

    Task<ApiFunction?> GetActiveByHttpMethodAndRouteAsync(
        ApiHttpMethod httpMethod,
        string routeTemplate,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<List<ApiFunction>> GetEffectiveByUserAsync(Guid organizationId, Guid userId, DateTimeOffset at, CancellationToken cancellationToken);
}
