namespace Nexora.Domain.Security;

public sealed class UserProviderKey
{
    private UserProviderKey() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public byte[] Ciphertext { get; private set; } = [];
    public byte[] Sha256 { get; private set; } = [];
    public string EndpointId { get; private set; } = string.Empty;
    public string MaskedSuffix { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public static UserProviderKey Create(Guid userId, string provider, byte[] ciphertext, byte[] sha256,
        string endpointId, string maskedSuffix, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, Provider = provider, Ciphertext = ciphertext,
        Sha256 = sha256, EndpointId = endpointId, MaskedSuffix = maskedSuffix,
        CreatedAt = now, UpdatedAt = now
    };

    public void Replace(byte[] ciphertext, byte[] sha256, string endpointId, string maskedSuffix, DateTimeOffset now)
    {
        Ciphertext = ciphertext;
        Sha256 = sha256;
        EndpointId = endpointId;
        MaskedSuffix = maskedSuffix;
        UpdatedAt = now;
    }

    public void MarkUsed(DateTimeOffset now) => LastUsedAt = now;
}
