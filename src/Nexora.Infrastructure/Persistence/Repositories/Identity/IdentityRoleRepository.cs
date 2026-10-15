using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Infrastructure.Services.Identity;

namespace Nexora.Infrastructure.Persistence.Repositories.Identity;

internal sealed class IdentityRoleRepository(NexoraDbContext dbContext, IUserContext userContext,
    IClock clock) : BaseRepository<ApplicationRole>(dbContext, userContext, clock), IRoleRepository
{
    public async Task<BasePaginationEntity<IdentityRoleSnapshot>> ListPageAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = DbContext.Roles.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(role => role.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new BasePaginationEntity<IdentityRoleSnapshot>
        {
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            Data = rows.Select(ToSnapshot).ToArray()
        };
    }

    public async Task<IdentityRoleSnapshot?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken) =>
        (await DbContext.Roles.AsNoTracking().SingleOrDefaultAsync(role => role.Id == roleId, cancellationToken)) is { } role ? ToSnapshot(role) : null;

    public async Task<IdentityRoleSnapshot?> GetActiveByIdAsync(Guid roleId, CancellationToken cancellationToken) =>
        (await DbContext.Roles.AsNoTracking().SingleOrDefaultAsync(role => role.Id == roleId && role.IsActive, cancellationToken)) is { } role ? ToSnapshot(role) : null;

    public async Task<IReadOnlyList<IdentityRoleSnapshot>> GetByIdsAsync(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken) =>
        (await DbContext.Roles.AsNoTracking().Where(role => roleIds.Contains(role.Id)).OrderBy(role => role.Name).ToListAsync(cancellationToken)).Select(ToSnapshot).ToArray();

    public Task<bool> ExistsWithNormalizedNameAsync(string normalizedName, Guid? excludedRoleId, CancellationToken cancellationToken) =>
        DbContext.Roles.AsNoTracking().AnyAsync(role => role.NormalizedName == normalizedName && (!excludedRoleId.HasValue || role.Id != excludedRoleId.Value), cancellationToken);

    public IdentityRoleSnapshot Create(string name, string normalizedName)
    {
        var role = new ApplicationRole { Id = Guid.NewGuid(), Name = name, NormalizedName = normalizedName, IsActive = true, ConcurrencyStamp = Guid.NewGuid().ToString() };
        DbContext.Roles.Add(role);
        return ToSnapshot(role);
    }

    public async Task RenameAsync(Guid roleId, string name, string normalizedName, CancellationToken cancellationToken)
    {
        var role = await DbContext.Roles.SingleAsync(item => item.Id == roleId, cancellationToken);
        role.Name = name;
        role.NormalizedName = normalizedName;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    public async Task SetActiveAsync(Guid roleId, bool active, CancellationToken cancellationToken)
    {
        var role = await DbContext.Roles.SingleAsync(item => item.Id == roleId, cancellationToken);
        role.IsActive = active;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();
    }

    private static IdentityRoleSnapshot ToSnapshot(ApplicationRole role) => new(role.Id, role.Name ?? string.Empty, role.IsActive);
}
