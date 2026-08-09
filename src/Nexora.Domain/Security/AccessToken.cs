using System.Net;
using Nexora.Domain.Common;
using Nexora.Domain.Common.Entities;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Security;

public class AccessToken : ConcurrencyEntityBase
{
    private AccessToken(Guid id)
        : base(id)
    {
    }

    protected AccessToken()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid UserId { get; private set; }

    public byte[] TokenHash { get; private set; } = Array.Empty<byte>();

    public string TokenType { get; set; } = string.Empty;

    public string? TokenName { get; set; }

    public string? Jti { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? RevokedByUserId { get; private set; }

    public string? RevokeReason { get; set; }

    public IPAddress? CreatedIp { get; set; }

    public IPAddress? LastUsedIp { get; set; }

    public string? UserAgent { get; set; }

    public static AccessToken CreateRefreshToken(
        Guid organizationId,
        Guid userId,
        byte[] tokenHash,
        string jti,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        IPAddress? createdIp,
        string? userAgent)
    {
        DomainGuard.RequiredId(organizationId, nameof(organizationId));
        DomainGuard.RequiredId(userId, nameof(userId));

        if (tokenHash is null || tokenHash.Length != 32)
        {
            throw new DomainException(
                "access_token.invalid_hash",
                "Token hash phải có độ dài 32 bytes.");
        }

        var utcIssuedAt = DomainGuard.Utc(issuedAt, nameof(issuedAt));
        var utcExpiresAt = DomainGuard.Utc(expiresAt, nameof(expiresAt));
        if (utcIssuedAt >= utcExpiresAt)
        {
            throw new DomainException(
                "access_token.invalid_range",
                "Thời điểm hết hạn phải sau thời điểm phát hành.");
        }

        var token = new AccessToken(Guid.NewGuid())
        {
            OrganizationId = organizationId,
            UserId = userId,
            TokenHash = tokenHash.ToArray(),
            TokenType = "REFRESH",
            Jti = DomainGuard.RequiredText(jti, nameof(jti), 150),
            IssuedAt = utcIssuedAt,
            ExpiresAt = utcExpiresAt,
            CreatedIp = createdIp,
            UserAgent = userAgent
        };

        return token;
    }

    public bool IsActiveAt(DateTimeOffset at)
    {
        var utcAt = DomainGuard.Utc(at, nameof(at));
        return RevokedAt is null && utcAt < ExpiresAt;
    }

    public void MarkUsed(DateTimeOffset usedAt, IPAddress? ipAddress)
    {
        if (!IsActiveAt(usedAt))
        {
            throw new DomainException(
                "access_token.inactive",
                "Refresh token không còn hoạt động.");
        }

        LastUsedAt = DomainGuard.Utc(usedAt, nameof(usedAt));
        LastUsedIp = ipAddress;
    }

    public void Revoke(Guid revokedByUserId, DateTimeOffset revokedAt, string reason)
    {
        DomainGuard.RequiredId(revokedByUserId, nameof(revokedByUserId));
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = DomainGuard.Utc(revokedAt, nameof(revokedAt));
        RevokedByUserId = revokedByUserId;
        RevokeReason = DomainGuard.RequiredText(reason, nameof(reason), 300);
    }
}
