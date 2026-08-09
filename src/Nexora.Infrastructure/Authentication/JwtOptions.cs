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
            throw new InvalidOperationException("JWT issuer and audience are required.");
        }

        if (string.IsNullOrWhiteSpace(SigningKey) || SigningKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signature private key must contain at least 32 characters.");
        }

        if (AccessTokenMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("JWT access token lifetime must be between 1 and 1440 minutes.");
        }

        if (RefreshTokenDays is < 1 or > 365)
        {
            throw new InvalidOperationException("JWT refresh token lifetime must be between 1 and 365 days.");
        }
    }
}
