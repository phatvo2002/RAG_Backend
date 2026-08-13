using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Infrastructure.Identity;

namespace Nexora.Infrastructure.Persistence.Repositories.Identity;

internal sealed class IdentityRoleRepository(
    NexoraDbContext dbContext,
    IUserContext userContext,
    IClock clock)
    : BaseRepository<ApplicationRole>(dbContext, userContext, clock), IRoleRepository;
