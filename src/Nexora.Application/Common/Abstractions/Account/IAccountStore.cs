using Nexora.Application.Features.V1.Account.Model;

namespace Nexora.Application.Common.Abstractions.Account;

public interface IAccountStore
{
    Task<AccountProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveProfileAsync(Guid userId, string displayName, string language, string? timeZone, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProviderKeyRecord>> GetKeysAsync(Guid userId, CancellationToken cancellationToken);
    Task<ProviderKeyRecord?> GetKeyAsync(Guid userId, string provider, CancellationToken cancellationToken);
    Task<ProviderKeyRecord> UpsertKeyAsync(Guid userId, string provider, byte[] ciphertext, byte[] hash, string endpointId, string suffix, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> DeleteKeyAsync(Guid userId, string provider, CancellationToken cancellationToken);
    Task MarkKeyUsedAsync(Guid userId, string provider, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IProviderKeyCipher
{
    bool IsConfigured { get; }
    byte[] Encrypt(Guid userId, string provider, string apiKey);
    string Decrypt(Guid userId, string provider, byte[] ciphertext);
}

public interface IProviderEndpointSettings
{
    IReadOnlyDictionary<string, string> ClaudeEndpoints { get; }
}
