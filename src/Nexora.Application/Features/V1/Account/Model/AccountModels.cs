namespace Nexora.Application.Features.V1.Account.Model;

public sealed record AccountProfileDto(string Email, string UserName, string DisplayName, string PreferredLanguage, string? TimeZone);
public sealed record ProviderKeyStatusDto(string Provider, string EndpointId, string MaskedSuffix, DateTimeOffset UpdatedAt, DateTimeOffset? LastUsedAt);
public sealed record ProviderEndpointDto(string Id, string Name);
public sealed record ProviderKeySettingsDto(IReadOnlyList<ProviderKeyStatusDto> Keys, IReadOnlyList<ProviderEndpointDto> ClaudeEndpoints, string? ConfigurationError);
public sealed record ProviderKeyRecord(string Provider, string EndpointId, string MaskedSuffix, byte[] Ciphertext, DateTimeOffset UpdatedAt, DateTimeOffset? LastUsedAt);
