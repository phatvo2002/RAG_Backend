using Microsoft.EntityFrameworkCore;
using Nexora.Application.Common.Abstractions.Account;
using Nexora.Application.Features.V1.Account.Model;
using Nexora.Domain.Common.ValueObjects;
using Nexora.Domain.Organizations;
using Nexora.Domain.Security;

namespace Nexora.Infrastructure.Persistence.Repositories.Identity;

internal sealed class AccountSettingsStore(NexoraDbContext db) : IAccountStore
{
    public async Task<AccountProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return new AccountProfileDto(user.Email ?? "", user.UserName ?? "", profile?.DisplayName ?? user.UserName ?? "",
            profile?.PreferredLanguage.Value ?? "en", profile?.TimeZone?.Value);
    }

    public async Task SaveProfileAsync(Guid userId, string displayName, string language, string? timeZone, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var languageCode = LanguageCode.Create(language);
        var zone = string.IsNullOrWhiteSpace(timeZone) ? null : TimeZoneId.Create(timeZone);
        if (profile is null)
            db.UserProfiles.Add(UserProfile.Create(userId, displayName, languageCode, zone, now));
        else
        {
            profile.UpdateDisplayName(displayName);
            profile.UpdatePreferredLanguage(languageCode);
            profile.UpdateTimeZone(zone);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderKeyRecord>> GetKeysAsync(Guid userId, CancellationToken cancellationToken) =>
        (await db.UserProviderKeys.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(cancellationToken)).Select(ToRecord).ToArray();

    public async Task<ProviderKeyRecord?> GetKeyAsync(Guid userId, string provider, CancellationToken cancellationToken) =>
        (await db.UserProviderKeys.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.Provider == provider, cancellationToken)) is { } row ? ToRecord(row) : null;

    public async Task<ProviderKeyRecord> UpsertKeyAsync(Guid userId, string provider, byte[] ciphertext, byte[] hash, string endpointId, string suffix, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = await db.UserProviderKeys.SingleOrDefaultAsync(x => x.UserId == userId && x.Provider == provider, cancellationToken);
        if (row is null)
        {
            row = UserProviderKey.Create(userId, provider, ciphertext, hash, endpointId, suffix, now);
            db.UserProviderKeys.Add(row);
        }
        else row.Replace(ciphertext, hash, endpointId, suffix, now);
        await db.SaveChangesAsync(cancellationToken);
        return ToRecord(row);
    }

    public async Task<bool> DeleteKeyAsync(Guid userId, string provider, CancellationToken cancellationToken)
    {
        var row = await db.UserProviderKeys.SingleOrDefaultAsync(x => x.UserId == userId && x.Provider == provider, cancellationToken);
        if (row is null) return false;
        db.UserProviderKeys.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkKeyUsedAsync(Guid userId, string provider, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = await db.UserProviderKeys.SingleAsync(x => x.UserId == userId && x.Provider == provider, cancellationToken);
        row.MarkUsed(now);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static ProviderKeyRecord ToRecord(UserProviderKey row) =>
        new(row.Provider, row.EndpointId, row.MaskedSuffix, row.Ciphertext, row.UpdatedAt, row.LastUsedAt);
}
