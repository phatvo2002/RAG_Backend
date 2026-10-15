using System.Security.Cryptography;
using System.Text;
using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Account;
using Nexora.Application.Features.V1.Account.Model;
using Nexora.Application.Features.V1.Chat.Model;

namespace Nexora.Application.Features.V1.Account;

public sealed record GetAccountProfileQuery : IRequest<AccountProfileDto>;
public sealed class GetAccountProfileQueryHandler(IAccountStore store, IUserContext userContext) : IRequestHandler<GetAccountProfileQuery, AccountProfileDto>
{
    public Task<AccountProfileDto> Handle(GetAccountProfileQuery request, CancellationToken cancellationToken) =>
        store.GetProfileAsync(AccountPolicy.UserId(userContext), cancellationToken);
}

public sealed record UpdateAccountProfileCommand(string DisplayName, string PreferredLanguage, string? TimeZone) : IRequest<AccountProfileDto>;
public sealed class UpdateAccountProfileCommandHandler(IAccountStore store, IUserContext userContext, IClock clock) : IRequestHandler<UpdateAccountProfileCommand, AccountProfileDto>
{
    public async Task<AccountProfileDto> Handle(UpdateAccountProfileCommand request, CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName?.Trim() ?? string.Empty;
        var timeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? null : request.TimeZone.Trim();
        if (displayName.Length is < 1 or > 200 || request.PreferredLanguage is not ("vi" or "en") || timeZone?.Length > 50)
            throw new ArgumentException("Invalid profile information.");
        var userId = AccountPolicy.UserId(userContext);
        await store.SaveProfileAsync(userId, displayName, request.PreferredLanguage, timeZone, clock.UtcNow, cancellationToken);
        return await store.GetProfileAsync(userId, cancellationToken);
    }
}

public sealed record GetProviderKeysQuery : IRequest<ProviderKeySettingsDto>;
public sealed class GetProviderKeysQueryHandler(IAccountStore store, IUserContext userContext, IProviderKeyCipher cipher, IProviderEndpointSettings endpoints) : IRequestHandler<GetProviderKeysQuery, ProviderKeySettingsDto>
{
    public async Task<ProviderKeySettingsDto> Handle(GetProviderKeysQuery request, CancellationToken cancellationToken)
    {
        var rows = await store.GetKeysAsync(AccountPolicy.UserId(userContext), cancellationToken);
        var allowed = AccountPolicy.AllowedClaudeEndpoints(endpoints);
        return new ProviderKeySettingsDto(rows.Select(AccountPolicy.ToStatus).ToArray(),
            allowed.Keys.OrderBy(x => x).Select(x => new ProviderEndpointDto(x, x == "official" ? "Claude (official)" : x)).ToArray(),
            cipher.IsConfigured ? null : "Chưa cấu hình khóa mã hóa API trên máy chủ.");
    }
}

public sealed record SaveProviderKeyCommand(string Provider, string ApiKey, string? EndpointId) : IRequest<ProviderKeyStatusDto?>;
public sealed class SaveProviderKeyCommandHandler(IAccountStore store, IUserContext userContext, IProviderKeyCipher cipher, IProviderEndpointSettings endpoints, IClock clock) : IRequestHandler<SaveProviderKeyCommand, ProviderKeyStatusDto?>
{
    public async Task<ProviderKeyStatusDto?> Handle(SaveProviderKeyCommand request, CancellationToken cancellationToken)
    {
        if (!cipher.IsConfigured) return null;
        var provider = AccountPolicy.NormalizeProvider(request.Provider);
        if (string.IsNullOrWhiteSpace(request.ApiKey)) throw new ArgumentException("Invalid provider API key.");
        var apiKey = request.ApiKey.Trim();
        if (apiKey.Length is < 10 or > 4096 || apiKey.Any(char.IsControl))
            throw new ArgumentException("Invalid provider API key.");
        var endpointId = provider == "gemini" ? "official" : request.EndpointId ?? "official";
        if (provider == "claude" && !AccountPolicy.AllowedClaudeEndpoints(endpoints).ContainsKey(endpointId))
            throw new ArgumentException("The selected Claude endpoint is unavailable.");
        var userId = AccountPolicy.UserId(userContext);
        var encrypted = cipher.Encrypt(userId, provider, apiKey);
        var keyBytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(keyBytes);
        CryptographicOperations.ZeroMemory(keyBytes);
        var row = await store.UpsertKeyAsync(userId, provider, encrypted, hash, endpointId, apiKey[^Math.Min(4, apiKey.Length)..], clock.UtcNow, cancellationToken);
        return AccountPolicy.ToStatus(row);
    }
}

public sealed record DeleteProviderKeyCommand(string Provider) : IRequest<bool>;
public sealed class DeleteProviderKeyCommandHandler(IAccountStore store, IUserContext userContext) : IRequestHandler<DeleteProviderKeyCommand, bool>
{
    public Task<bool> Handle(DeleteProviderKeyCommand request, CancellationToken cancellationToken) =>
        store.DeleteKeyAsync(AccountPolicy.UserId(userContext), AccountPolicy.NormalizeProvider(request.Provider), cancellationToken);
}

public sealed record GetProviderCredentialQuery(string Provider) : IRequest<ProviderCredential?>;
public sealed class GetProviderCredentialQueryHandler(IAccountStore store, IUserContext userContext, IProviderKeyCipher cipher, IProviderEndpointSettings endpoints, IClock clock) : IRequestHandler<GetProviderCredentialQuery, ProviderCredential?>
{
    public async Task<ProviderCredential?> Handle(GetProviderCredentialQuery request, CancellationToken cancellationToken)
    {
        var provider = AccountPolicy.NormalizeProvider(request.Provider);
        var userId = AccountPolicy.UserId(userContext);
        var row = await store.GetKeyAsync(userId, provider, cancellationToken);
        if (row is null) return null;
        var baseUrl = provider == "gemini" ? "https://generativelanguage.googleapis.com/v1beta" :
            AccountPolicy.AllowedClaudeEndpoints(endpoints).GetValueOrDefault(row.EndpointId);
        if (baseUrl is null) throw new InvalidOperationException("The saved Claude endpoint is no longer approved.");
        var apiKey = cipher.Decrypt(userId, provider, row.Ciphertext);
        await store.MarkKeyUsedAsync(userId, provider, clock.UtcNow, cancellationToken);
        return new ProviderCredential(apiKey, row.EndpointId, baseUrl);
    }
}

internal static class AccountPolicy
{
    public static Guid UserId(IUserContext context) => context.UserId ?? throw new UnauthorizedAccessException("Sign in is required.");
    public static string NormalizeProvider(string provider) => provider?.ToLowerInvariant() switch
    {
        "gemini" => "gemini", "claude" => "claude", _ => throw new ArgumentException("Unknown AI provider.")
    };
    public static ProviderKeyStatusDto ToStatus(ProviderKeyRecord row) =>
        new(row.Provider, row.EndpointId, $"••••{row.MaskedSuffix}", row.UpdatedAt, row.LastUsedAt);

    public static IReadOnlyDictionary<string, string> AllowedClaudeEndpoints(IProviderEndpointSettings settings)
    {
        var endpoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["official"] = "https://api.anthropic.com/v1" };
        foreach (var pair in settings.ClaudeEndpoints)
        {
            if (!Uri.TryCreate(pair.Value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
                throw new InvalidOperationException("ProviderKeys:ClaudeEndpoints contains an invalid HTTPS endpoint.");
            if (pair.Key == "official" && pair.Value.TrimEnd('/') != endpoints["official"])
                throw new InvalidOperationException("The official Claude endpoint cannot be overridden.");
            endpoints[pair.Key] = pair.Value.TrimEnd('/');
        }
        return endpoints;
    }
}
