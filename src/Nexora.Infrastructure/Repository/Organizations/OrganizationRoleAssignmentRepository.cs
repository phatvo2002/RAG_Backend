using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Organizations;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationRoleAssignmentRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<OrganizationRoleAssignment>(dbContext, userContext, clock), IOrganizationRoleAssignmentRepository
{
    public Task<OrganizationRoleAssignment?> GetAsync(Guid organizationId, Guid userId, Guid roleId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = DbContext.OrganizationRoleAssignments.IgnoreQueryFilters().Where(assignment => assignment.OrganizationId == organizationId && assignment.UserId == userId && assignment.RoleId == roleId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<OrganizationRoleAssignment>> GetByUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        DbContext.OrganizationRoleAssignments.IgnoreQueryFilters().Where(assignment => assignment.OrganizationId == organizationId && assignment.UserId == userId).AsNoTracking().ToListAsync(cancellationToken);

}
