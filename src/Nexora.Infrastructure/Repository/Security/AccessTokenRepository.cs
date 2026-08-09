using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Nexora.Domain.Security;

namespace Nexora.Infrastructure.Persistence.Repositories;

internal sealed class AccessTokenRepository(NexoraDbContext dbContext, IUserContext userContext, IClock clock)
    : BaseRepository<AccessToken>(dbContext, userContext, clock), IAccessTokenRepository
{
    public Task<AccessToken?> GetRefreshTokenAsync(byte[] tokenHash, CancellationToken cancellationToken) =>
        DbContext.AccessTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(token => token.TokenType == "REFRESH" && token.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeRefreshTokensAsync(
        Guid userId,
        Guid organizationId,
        Guid revokedByUserId,
        DateTimeOffset revokedAt,
        string reason,
        CancellationToken cancellationToken)
    {
        var tokens = await DbContext.AccessTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == userId && token.OrganizationId == organizationId && token.TokenType == "REFRESH" && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(revokedByUserId, revokedAt, reason);
        }
    }
}
