using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Authorization;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class RoleApiFunctionRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<RoleApiFunction>(dbContext, userContext, clock), IRoleApiFunctionRepository
{
    public Task<RoleApiFunction?> GetAsync(Guid organizationId, Guid roleId, Guid functionId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = DbContext.RoleApiFunctions.IgnoreQueryFilters().Where(grant => grant.OrganizationId == organizationId && grant.RoleId == roleId && grant.FunctionId == functionId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<RoleApiFunction>> GetByRoleAsync(Guid organizationId, Guid roleId, CancellationToken cancellationToken) =>
        DbContext.RoleApiFunctions.IgnoreQueryFilters().Where(grant => grant.OrganizationId == organizationId && grant.RoleId == roleId).AsNoTracking().ToListAsync(cancellationToken);

}
