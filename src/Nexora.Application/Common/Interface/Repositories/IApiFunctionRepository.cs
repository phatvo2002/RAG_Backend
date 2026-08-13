using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IApiFunctionRepository : IBaseRepository<ApiFunction>
{
    Task<ApiFunction?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task<ApiFunction?> GetActiveByHttpMethodAndRouteAsync(
        ApiHttpMethod httpMethod,
        string routeTemplate,
        Guid? excludingId,
        CancellationToken cancellationToken);

}
