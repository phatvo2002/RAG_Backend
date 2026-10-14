using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Authorization;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations.Enums;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class ApiFunctionRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<ApiFunction>(dbContext, userContext, clock), IApiFunctionRepository
{
    public Task<ApiFunction?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Query().SingleOrDefaultAsync(
            function => function.Code == ApiFunctionCode.Create(code),
            cancellationToken);

    public Task<List<ApiFunction>> GetActiveByIdsAsync(IReadOnlyCollection<Guid> functionIds, CancellationToken cancellationToken) =>
        Query().AsNoTracking().Where(function => function.Status == ApiFunctionStatus.Active && functionIds.Contains(function.Id)).ToListAsync(cancellationToken);

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

    public Task<List<ApiFunction>> GetActiveMenuParentsAsync(CancellationToken cancellationToken)
    {
        var menuRouteTemplate = RouteTemplate.Create("#");
        return Query()
            .AsNoTracking()
            .Where(function => function.Status == ApiFunctionStatus.Active && function.RouteTemplate == menuRouteTemplate)
            .OrderBy(function => function.DisplayOrder)
            .ThenBy(function => function.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ApiFunction>> GetEffectiveByUserAsync(Guid organizationId, Guid userId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var effectiveRoleIds = DbContext.OrganizationRoleAssignments
            .IgnoreQueryFilters()
            .Where(assignment =>
                assignment.OrganizationId == organizationId &&
                assignment.UserId == userId &&
                assignment.Status == OrganizationRoleAssignmentStatus.Active &&
                assignment.ValidFrom <= at &&
                (assignment.ExpiresAt == null || assignment.ExpiresAt > at))
            .Select(assignment => assignment.RoleId);

        var effectiveGrants = DbContext.RoleApiFunctions
            .IgnoreQueryFilters()
            .Where(grant =>
                grant.OrganizationId == organizationId &&
                effectiveRoleIds.Contains(grant.RoleId) &&
                grant.RevokedAt == null &&
                grant.ValidFrom <= at &&
                (grant.ExpiresAt == null || grant.ExpiresAt > at) &&
                DbContext.Set<ApplicationRole>().Any(role => role.Id == grant.RoleId && role.IsActive));

        var allowedFunctionIds = effectiveGrants
            .Where(grant => grant.Effect == PermissionEffect.Allow)
            .Select(grant => grant.FunctionId);
        var deniedFunctionIds = effectiveGrants
            .Where(grant => grant.Effect == PermissionEffect.Deny)
            .Select(grant => grant.FunctionId);
        var menuRouteTemplate = RouteTemplate.Create("#");

        return DbContext.ApiFunctions
            .AsNoTracking()
            .Where(function =>
                function.Status == ApiFunctionStatus.Active &&
                function.RouteTemplate != menuRouteTemplate &&
                allowedFunctionIds.Contains(function.Id) &&
                !deniedFunctionIds.Contains(function.Id))
            .OrderBy(function => function.Module)
            .ThenBy(function => function.Name)
            .ToListAsync(cancellationToken);
    }

}
