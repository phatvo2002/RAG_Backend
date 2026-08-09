using Nexora.Domain.Organizations;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IUserProfileRepository : IBaseRepository<UserProfile>
{
    Task<UserProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task AddAsync(
        UserProfile profile,
        CancellationToken cancellationToken);
}
