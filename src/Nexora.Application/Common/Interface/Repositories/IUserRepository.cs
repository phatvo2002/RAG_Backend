using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IUserRepository
{
    Task<BasePaginationEntity<IdentityUserSnapshot>> ListPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<IdentityUserSnapshot?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IdentityUserSnapshot?> GetActiveByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<IdentityUserSnapshot>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<IdentityUserSnapshot>> GetByRoleAsync(Guid organizationId, Guid roleId, DateTimeOffset utcNow, CancellationToken cancellationToken);
    Task<bool> ExistsWithEmailOrUserNameAsync(Guid excludedUserId, string normalizedEmail, string normalizedUserName, CancellationToken cancellationToken);
    Task SetDetailsAsync(Guid userId, string email, string userName, string? phoneNumber, CancellationToken cancellationToken);
    Task SetActiveAsync(Guid userId, bool active, CancellationToken cancellationToken);
    Task<IdentityUserSnapshot?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    Task<Result<IdentityUserSnapshot>> CreateWithPasswordAsync(
        string email,
        string userName,
        string password,
        string? phoneNumber,
        CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);
}
