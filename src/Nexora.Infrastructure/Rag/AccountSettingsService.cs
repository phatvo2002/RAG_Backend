using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Features.V1.Chat.Model;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
using Nexora.Domain.Security;
using Nexora.Infrastructure.Options;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Rag;

public sealed record AccountProfileDto(string Email, string UserName, string DisplayName, string PreferredLanguage, string? TimeZone);
public sealed record ProviderKeyStatusDto(string Provider, string EndpointId, string MaskedSuffix,
    DateTimeOffset UpdatedAt, DateTimeOffset? LastUsedAt);
public sealed record ProviderEndpointDto(string Id, string Name);
public sealed record ProviderKeySettingsDto(IReadOnlyList<ProviderKeyStatusDto> Keys,
    IReadOnlyList<ProviderEndpointDto> ClaudeEndpoints, string? ConfigurationError);
public sealed class AccountSettingsService(
    NexoraDbContext db, IUserContext userContext, IClock clock,
    ProviderKeyCipher cipher, IOptions<ProviderKeyOptions> providerOptions)
{
    private Guid UserId => userContext.UserId ?? throw new UnauthorizedAccessException("Sign in is required.");

    public async Task<AccountProfileDto> GetProfileAsync(CancellationToken ct)
    {
        var id = UserId;
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == id && x.IsActive, ct);
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id, ct);
        return new AccountProfileDto(user.Email ?? "", user.UserName ?? "", profile?.DisplayName ?? user.UserName ?? "",
            profile?.PreferredLanguage.Value ?? "en", profile?.TimeZone?.Value);
    }

    public async Task<AccountProfileDto> UpdateProfileAsync(string displayName, string preferredLanguage, string? timeZone, CancellationToken ct)
    {
        var id = UserId;
        displayName = displayName?.Trim() ?? string.Empty;
        if (displayName.Length is < 1 or > 200 || preferredLanguage is not ("vi" or "en") || timeZone?.Length > 50)
            throw new ArgumentException("Invalid profile information.");
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == id, ct);
        var language = LanguageCode.Create(preferredLanguage);
        var zone = string.IsNullOrWhiteSpace(timeZone) ? null : TimeZoneId.Create(timeZone.Trim());
        if (profile is null)
            db.UserProfiles.Add(UserProfile.Create(id, displayName, language, zone, clock.UtcNow));
        else
        {
            profile.UpdateDisplayName(displayName);
            profile.UpdatePreferredLanguage(language);
            profile.UpdateTimeZone(zone);
        }
        await db.SaveChangesAsync(ct);
        return await GetProfileAsync(ct);
    }

    public async Task<ProviderKeySettingsDto> ListKeysAsync(CancellationToken ct)
    {
        var id = UserId;
        var rows = await db.Set<UserProviderKey>().AsNoTracking().Where(x => x.UserId == id).ToListAsync(ct);
        return new ProviderKeySettingsDto(rows.Select(ToStatus).ToArray(),
            AllowedClaudeEndpoints().Keys.OrderBy(x => x).Select(x => new ProviderEndpointDto(x, x == "official" ? "Claude (official)" : x)).ToArray(),
            cipher.IsConfigured ? null : "Chưa cấu hình khóa mã hóa API trên máy chủ.");
    }

    public async Task<ProviderKeyStatusDto> SaveKeyAsync(string provider, string apiKey, string? endpointId, CancellationToken ct)
    {
        provider = NormalizeProvider(provider);
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("Invalid provider API key.");
        apiKey = apiKey.Trim();
        if (apiKey.Length is < 10 or > 4096 || apiKey.Any(char.IsControl))
            throw new ArgumentException("Invalid provider API key.");
        endpointId = provider == "gemini" ? "official" : endpointId ?? "official";
        if (provider == "claude" && !AllowedClaudeEndpoints().ContainsKey(endpointId))
            throw new ArgumentException("The selected Claude endpoint is unavailable.");
        var id = UserId;
        var encrypted = cipher.Encrypt(id, provider, apiKey);
        var keyBytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(keyBytes);
        CryptographicOperations.ZeroMemory(keyBytes);
        var suffix = apiKey[^Math.Min(4, apiKey.Length)..];
        var row = await db.Set<UserProviderKey>().SingleOrDefaultAsync(x => x.UserId == id && x.Provider == provider, ct);
        if (row is null)
        {
            row = UserProviderKey.Create(id, provider, encrypted, hash, endpointId, suffix, clock.UtcNow);
            db.Set<UserProviderKey>().Add(row);
        }
        else row.Replace(encrypted, hash, endpointId, suffix, clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return ToStatus(row);
    }

    public async Task<bool> DeleteKeyAsync(string provider, CancellationToken ct)
    {
        provider = NormalizeProvider(provider);
        var id = UserId;
        var row = await db.Set<UserProviderKey>().SingleOrDefaultAsync(x => x.UserId == id && x.Provider == provider, ct);
        if (row is null) return false;
        db.Set<UserProviderKey>().Remove(row);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ProviderCredential?> GetCredentialAsync(string provider, CancellationToken ct)
    {
        provider = NormalizeProvider(provider);
        var id = UserId;
        var row = await db.Set<UserProviderKey>().SingleOrDefaultAsync(x => x.UserId == id && x.Provider == provider, ct);
        if (row is null) return null;
        var baseUrl = provider == "gemini" ? "https://generativelanguage.googleapis.com/v1beta" :
            AllowedClaudeEndpoints().GetValueOrDefault(row.EndpointId);
        if (baseUrl is null) throw new InvalidOperationException("The saved Claude endpoint is no longer approved.");
        var key = cipher.Decrypt(id, provider, row.Ciphertext);
        row.MarkUsed(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return new ProviderCredential(key, row.EndpointId, baseUrl);
    }

    private IReadOnlyDictionary<string, string> AllowedClaudeEndpoints()
    {
        var endpoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["official"] = "https://api.anthropic.com/v1"
        };
        foreach (var pair in providerOptions.Value.ClaudeEndpoints)
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

    private static string NormalizeProvider(string provider) => provider.ToLowerInvariant() switch
    {
        "gemini" => "gemini", "claude" => "claude", _ => throw new ArgumentException("Unknown AI provider.")
    };

    private static ProviderKeyStatusDto ToStatus(UserProviderKey row) =>
        new(row.Provider, row.EndpointId, $"••••{row.MaskedSuffix}", row.UpdatedAt, row.LastUsedAt);
}
