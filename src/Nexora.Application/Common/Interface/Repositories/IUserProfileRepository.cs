using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IUserProfileRepository : IBaseRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken,
        bool asTracking = false);

    Task AddAsync(
        UserProfile profile,
        CancellationToken cancellationToken);
}
