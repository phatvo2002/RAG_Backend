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

namespace Nexora.Infrastructure.Services.Authentication;

internal sealed class AuthTokenService(
    IOptions<JwtOptions> options,
    IClock clock,
    IUnitOfWork unitOfWork,
    IDefaultOrganizationProvider organizationProvider)
    : IAuthTokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<AuthTokenSet> IssueAsync(IdentityUserSnapshot user, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _options.Validate();

        var organizationId = await organizationProvider.GetOrganizationIdAsync(cancellationToken)
            ?? throw new InvalidOperationException(CommonMessages.User.DefaultOrganizationNotConfigured);

        var now = clock.UtcNow;
        var accessTokenExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshTokenExpiresAt = now.AddDays(_options.RefreshTokenDays);
        var roleId = await GetEffectiveRoleIdAsync(user.UserId, organizationId, now, cancellationToken);
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

        unitOfWork.AccessTokenRepository.Insert(refreshToken);

        var accessToken = CreateAccessToken(
            user,
            organizationId,
            roleId,
            refreshTokenId,
            now,
            accessTokenExpiresAt);

        return new AuthTokenSet(accessToken, accessTokenExpiresAt, rawRefreshToken, refreshTokenExpiresAt, user.UserId, organizationId);
    }

    public async Task<AuthTokenSet?> RotateRefreshTokenAsync(string refreshToken, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        _options.Validate();

        var now = clock.UtcNow;
        var token = await unitOfWork.AccessTokenRepository.GetRefreshTokenAsync(HashToken(refreshToken), cancellationToken);

        if (token is null)
        {
            return null;
        }

        if (token.RevokedAt is not null)
        {
            await unitOfWork.AccessTokenRepository.RevokeRefreshTokensAsync(token.UserId, token.OrganizationId, token.UserId, now, CommonMessages.TokenReasons.RefreshTokenReuseDetected, cancellationToken);
            return null;
        }

        if (!token.IsActiveAt(now))
        {
            token.Revoke(token.UserId, now, CommonMessages.TokenReasons.RefreshTokenExpired);
            return null;
        }

        var userEntity = await unitOfWork.UserRepository.GetActiveByIdAsync(token.UserId, cancellationToken);
        if (userEntity is null)
        {
            token.Revoke(token.UserId, now, CommonMessages.TokenReasons.RefreshTokenIdentityInvalid);
            return null;
        }

        var user = new IdentityUserSnapshot(
            userEntity.Id,
            userEntity.Email ?? userEntity.UserName ?? string.Empty,
            userEntity.UserName ?? userEntity.Email ?? string.Empty,
            userEntity.PhoneNumber,
            userEntity.IsActive);

        token.MarkUsed(now, ParseIpAddress(ipAddress));
        token.Revoke(token.UserId, now, CommonMessages.TokenReasons.RefreshTokenRotated);

        return await IssueAsync(
            user,
            ipAddress,
            userAgent,
            cancellationToken);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, Guid? revokedByUserId, string reason, CancellationToken cancellationToken)
    {
        var token = await unitOfWork.AccessTokenRepository.GetRefreshTokenAsync(HashToken(refreshToken), cancellationToken);

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

    private async Task<Guid?> GetEffectiveRoleIdAsync(
        Guid userId,
        Guid organizationId,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        var assignments = await unitOfWork.OrganizationRoleAssignmentRepository.GetByUserAsync(
            organizationId,
            userId,
            cancellationToken);

        return assignments
            .Where(assignment => assignment.IsEffectiveAt(at))
            .OrderByDescending(assignment => assignment.ValidFrom)
            .Select(assignment => (Guid?)assignment.RoleId)
            .FirstOrDefault();
    }

    private string CreateAccessToken(
        IdentityUserSnapshot user,
        Guid organizationId,
        Guid? roleId,
        Guid tokenId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim("organizationId", organizationId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString("N")),
            new Claim("tokenType", "ACCESS")
        };

        if (roleId.HasValue)
        {
            claims.Add(new Claim("roleId", roleId.Value.ToString()));
        }

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
