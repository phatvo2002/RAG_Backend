using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<Department>(dbContext, userContext, clock), IDepartmentRepository
{
    public Task<Department?> GetByIdAsync(Guid departmentId, bool asTracking, CancellationToken cancellationToken)
    {
        var query = DbContext.Departments.IgnoreQueryFilters().Where(department => department.Id == departmentId);
        return (asTracking ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CodeExistsAsync(Guid organizationId, string code, Guid? excludingDepartmentId, CancellationToken cancellationToken)
    {
        var departmentCode = DepartmentCode.Create(code);
        return DbContext.Departments.IgnoreQueryFilters().AnyAsync(department => department.OrganizationId == organizationId && department.Code == departmentCode && (!excludingDepartmentId.HasValue || department.Id != excludingDepartmentId.Value), cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid departmentId, Guid organizationId, CancellationToken cancellationToken) =>
        DbContext.Departments.IgnoreQueryFilters().AnyAsync(department => department.Id == departmentId && department.OrganizationId == organizationId, cancellationToken);

    public Task<List<Department>> GetAllForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken) =>
        DbContext.Departments.IgnoreQueryFilters().Where(department => department.OrganizationId == organizationId).OrderBy(department => department.Name).AsNoTracking().ToListAsync(cancellationToken);

}
