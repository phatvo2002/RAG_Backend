using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.ValueObjects;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class ApiFunctionRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<ApiFunction>(dbContext, userContext, clock), IApiFunctionRepository
{
    public Task<ApiFunction?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Query().SingleOrDefaultAsync(
            function => function.Code == ApiFunctionCode.Create(code),
            cancellationToken);

    public Task<ApiFunction?> GetActiveByHttpMethodAndRouteAsync(
        ApiHttpMethod httpMethod,
        string routeTemplate,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(
            function =>
                function.Status == ApiFunctionStatus.Active &&
                function.HttpMethod == httpMethod &&
                function.RouteTemplate == RouteTemplate.Create(routeTemplate) &&
                (!excludingId.HasValue || function.Id != excludingId.Value),
            cancellationToken);

}
