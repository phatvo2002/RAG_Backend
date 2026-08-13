namespace Nexora.Application.Common.Abstractions.Auth;

public interface IAuthTokenService
{
    Task<AuthTokenSet> IssueAsync(
        IdentityUserSnapshot user,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<AuthTokenSet?> RotateRefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<bool> RevokeRefreshTokenAsync(
        string refreshToken,
        Guid? revokedByUserId,
        string reason,
        CancellationToken cancellationToken);
}

public sealed record AuthTokenSet(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid UserId);
