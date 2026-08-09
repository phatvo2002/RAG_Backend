using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Security;

namespace Nexora.Infrastructure.Authentication;

internal sealed class AuthTokenService(
    IOptions<JwtOptions> options,
    IClock clock,
    IIdentityService identityService,
    IOrganizationMembershipRepository membershipRepository,
    IAccessTokenRepository accessTokenRepository)
    : IAuthTokenService
{
    private readonly JwtOptions _options = options.Value;

    public Task<AuthTokenSet> IssueAsync(IdentityUserSnapshot user, Guid organizationId, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _options.Validate();

        var now = clock.UtcNow;
        var accessTokenExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshTokenExpiresAt = now.AddDays(_options.RefreshTokenDays);
        var refreshTokenId = Guid.NewGuid();
        var rawRefreshToken = CreateRawToken();
        var refreshTokenHash = HashToken(rawRefreshToken);
        var parsedIpAddress = ParseIpAddress(ipAddress);

        var refreshToken = AccessToken.CreateRefreshToken(
            organizationId,
            user.UserId,
            refreshTokenHash,
            refreshTokenId.ToString("N"),
            now,
            refreshTokenExpiresAt,
            parsedIpAddress,
            userAgent);

        accessTokenRepository.Insert(refreshToken);

        var accessToken = CreateAccessToken(
            user,
            organizationId,
            refreshTokenId,
            now,
            accessTokenExpiresAt);

        return Task.FromResult(
            new AuthTokenSet(accessToken, accessTokenExpiresAt, rawRefreshToken, refreshTokenExpiresAt, organizationId, user.UserId));
    }

    public async Task<AuthTokenSet?> RotateRefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        _options.Validate();

        var now = clock.UtcNow;
        var token = await accessTokenRepository.GetRefreshTokenAsync(HashToken(refreshToken), cancellationToken);

        if (token is null)
        {
            return null;
        }

        if (token.RevokedAt is not null)
        {
            await accessTokenRepository.RevokeRefreshTokensAsync(token.UserId, token.OrganizationId, token.UserId, now, CommonMessages.TokenReasons.RefreshTokenReuseDetected, cancellationToken);
            return null;
        }

        if (!token.IsActiveAt(now))
        {
            token.Revoke(token.UserId, now, CommonMessages.TokenReasons.RefreshTokenExpired);
            return null;
        }

        var user = await identityService.GetByIdAsync(token.UserId, cancellationToken);
        var membership = await membershipRepository.GetForAuthenticationAsync(token.OrganizationId, token.UserId, cancellationToken);

        if (user is null || membership is null)
        {
            token.Revoke(token.UserId, now, CommonMessages.TokenReasons.RefreshTokenIdentityInvalid);
            return null;
        }

        token.MarkUsed(now, ParseIpAddress(ipAddress));
        token.Revoke(token.UserId, now, CommonMessages.TokenReasons.RefreshTokenRotated);

        return await IssueAsync(
            user,
            token.OrganizationId,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, Guid? revokedByUserId, string reason, CancellationToken cancellationToken)
    {
        var token = await accessTokenRepository.GetRefreshTokenAsync(HashToken(refreshToken), cancellationToken);

        if (token is null || token.RevokedAt is not null)
        {
            return false;
        }

        if (revokedByUserId.HasValue && revokedByUserId.Value != token.UserId)
        {
            return false;
        }

        token.Revoke(revokedByUserId ?? token.UserId, clock.UtcNow, reason);
        return true;
    }

    private string CreateAccessToken(IdentityUserSnapshot user, Guid organizationId, Guid tokenId, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("organizationId", organizationId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString("N")),
            new Claim("tokenType", "ACCESS")
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateRawToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

    private static byte[] HashToken(string rawToken) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

    private static IPAddress? ParseIpAddress(string? ipAddress) =>
        IPAddress.TryParse(ipAddress, out var parsedIpAddress) ? parsedIpAddress : null;
}
