using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Organizations;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationRoleAssignmentRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<OrganizationRoleAssignment>(dbContext, userContext, clock), IOrganizationRoleAssignmentRepository
{
    public Task AddAsync(OrganizationRoleAssignment assignment, CancellationToken cancellationToken) =>
        base.AddEntityAsync(assignment, cancellationToken);
}
