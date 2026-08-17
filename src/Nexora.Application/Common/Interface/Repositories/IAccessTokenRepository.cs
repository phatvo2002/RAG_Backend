using Nexora.Domain.Security;

namespace Nexora.Application.Common.Abstractions.Repositories;

public interface IAccessTokenRepository : IBaseRepository<AccessToken>
{
    Task<AccessToken?> GetRefreshTokenAsync(byte[] tokenHash, CancellationToken cancellationToken);

    Task RevokeRefreshTokensAsync(
        Guid userId,
        Guid organizationId,
        Guid revokedByUserId,
        DateTimeOffset revokedAt,
        string reason,
        CancellationToken cancellationToken);
}
