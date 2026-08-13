using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Organizations;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentMembershipRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<DepartmentMembership>(dbContext, userContext, clock), IDepartmentMembershipRepository
{
    public Task<DepartmentMembership?> GetAsync(Guid organizationId, Guid departmentId, Guid userId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = DbContext.DepartmentMemberships.IgnoreQueryFilters().Where(membership => membership.OrganizationId == organizationId && membership.DepartmentId == departmentId && membership.UserId == userId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<DepartmentMembership>> GetByDepartmentAsync(Guid organizationId, Guid departmentId, CancellationToken cancellationToken) =>
        DbContext.DepartmentMemberships.IgnoreQueryFilters().Where(membership => membership.OrganizationId == organizationId && membership.DepartmentId == departmentId).AsNoTracking().ToListAsync(cancellationToken);

}
