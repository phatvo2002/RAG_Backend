using Nexora.Application.Common.Constants;

namespace Nexora.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SignaturePrivateKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;

    public string SigningKey => string.IsNullOrWhiteSpace(SignaturePrivateKey) ? SecretKey : SignaturePrivateKey;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException(CommonMessages.Infrastructure.JwtIssuerAudienceRequired);
        }

        if (string.IsNullOrWhiteSpace(SigningKey) || SigningKey.Length < 32)
        {
            throw new InvalidOperationException(CommonMessages.Infrastructure.JwtSigningKeyTooShort);
        }

        if (AccessTokenMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException(CommonMessages.Infrastructure.JwtAccessTokenLifetimeInvalid);
        }

        if (RefreshTokenDays is < 1 or > 365)
        {
            throw new InvalidOperationException(CommonMessages.Infrastructure.JwtRefreshTokenLifetimeInvalid);
        }
    }
}
