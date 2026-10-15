using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Abstractions;
using Nexora.Domain.Organizations;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class UserProfileRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<UserProfile>(dbContext, userContext, clock), IUserProfileRepository
{
    public Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken, bool asTracking = false) =>
        Query(asTracking).SingleOrDefaultAsync(
            profile => profile.UserId == userId,
            cancellationToken);

}
